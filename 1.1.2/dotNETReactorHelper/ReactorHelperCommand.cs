using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;
using Task = System.Threading.Tasks.Task;
using System.Collections;
using System.ComponentModel.Design;
using System.Reflection;
using System.Security.Cryptography;
using System;
using ReactorHelper;
using Microsoft.VisualStudio.Settings.Telemetry;
using System.Text;

namespace dotNETReactorHelper
{
    internal sealed class ReactorHelperCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("065e4edc-d973-47ad-b19f-909886a6eafc");
        private readonly AsyncPackage package;
        private IVsOutputWindowPane outputPane;

        public static List<string> outputPaths = new List<string>();

        private ReactorHelperCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            InitializeOutputPane();

            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.ExecuteAsync, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            new ReactorHelperCommand(package, commandService);
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            outputPaths.Clear();

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            if (dte != null)
            {
                try
                {
                    var solution = dte.Solution;
                    string solutionPath = solution.FullName;
                    string solutionDirectory = Path.GetDirectoryName(solutionPath);

                    string ConfigFilePath = Application.StartupPath + "\\citedllconfig.json";

                    // 获取当前活动配置是 "Debug" 还是 "Release"
                    string activeConfiguration = GetActiveConfiguration(dte);
                    await LogMessageAsync($"当前活动配置: {activeConfiguration}");

                    var csprojPaths = GetCsprojPaths(solutionPath, solutionDirectory);

                    // 自动混淆 .exe 和 .dll 文件
                    var exeAndDllPaths = csprojPaths.SelectMany(path => GetExeAndDllPaths(path, activeConfiguration)).ToList();
                    outputPaths.AddRange(exeAndDllPaths);

                    // 从dll文件夹和活动解决方案配置（Debug或Release）中获取引用的 .dll 文件路径
                    var referencedDllPaths = GetReferencedDllPaths(solutionDirectory, activeConfiguration).ToList();

                    //获取WinExe中的GUID
                    var exeGuid = GetWinExeProjectGuids(solutionPath);

                    //判断配置文件是否存在
                    string isExistsifm = IsExistsConfig(ConfigFilePath);
                    await LogMessageAsync($"{isExistsifm}");
                    

                    //打开 DisPlayForm 让用户选择要混淆的 DLL
                    List<string> guids = GetWinExeProjectGuids(solutionPath);
                    using (var displayForm = new DisPlayForm(exeAndDllPaths, referencedDllPaths, guids))
                    {
                        var result = displayForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            outputPaths.Clear();
                            outputPaths.AddRange(displayForm.SelectedDllPaths);
                            outputPaths.AddRange(displayForm.SelectedCiteDllPaths);
                        }
                        else
                        {
                            await LogMessageAsync("用户取消了选择，操作中止。");
                            return;
                        }
                    }

                    // 调试信息
                    var debugInfo = $"解决方案路径: {solutionPath}\n解决方案目录: {solutionDirectory}\n\nCSProj路径:\n{string.Join("\n", csprojPaths)}\n\n输出文件路径:\n{string.Join("\n", outputPaths)}";
                    await LogMessageAsync(debugInfo);

                    // 重新生成解决方案
                    await CleanAndBuildSolutionAsync(dte);

                    if (dte.Solution.SolutionBuild.LastBuildInfo == 0)
                    {
                        await LogMessageAsync("解决方案重新生成完成。");
                        await LogMessageAsync("开始执行代码混淆！");
                        await ReactorHelper.Instance.StartAsync(outputPaths, outputPane);
                    }
                    else
                    {
                        await LogMessageAsync("重新生成解决方案失败。");
                    }
                }
                catch (Exception ex)
                {
                    await LogMessageAsync($"执行过程中发生错误: {ex.Message}");
                }
            }
        }

        private IServiceProvider ServiceProvider => this.package;

        private void InitializeOutputPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IVsOutputWindow outputWindow = (IVsOutputWindow)ServiceProvider.GetService(typeof(SVsOutputWindow));
            Guid generalPaneGuid = VSConstants.GUID_OutWindowGeneralPane;
            outputWindow.GetPane(ref generalPaneGuid, out outputPane);

            if (outputPane == null)
            {
                outputWindow.CreatePane(ref generalPaneGuid, "dotNET Reactor Helper", 1, 1);
                outputWindow.GetPane(ref generalPaneGuid, out outputPane);
            }
        }

        private async Task LogMessageAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (outputPane != null)
            {
                outputPane.OutputString($"{message}\n");
                outputPane.Activate();
            }

            System.Diagnostics.Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        private static string GetActiveConfiguration(DTE2 dte)
        {
            return dte.Solution.SolutionBuild.ActiveConfiguration.Name;
        }

        private static string[] GetCsprojPaths(string solutionPath, string solutionDirectory)
        {
            var csprojPaths = new List<string>();

            foreach (var line in File.ReadLines(solutionPath))
            {
                var match = Regex.Match(line, @"""([^""]+\.csproj)""");
                if (match.Success)
                {
                    var relativeProjectPath = match.Groups[1].Value;
                    var absoluteProjectPath = Path.Combine(solutionDirectory, relativeProjectPath);
                    csprojPaths.Add(absoluteProjectPath);
                }
            }

            return csprojPaths.ToArray();
        }

        private static IEnumerable<string> GetExeAndDllPaths(string csprojFullPath, string activeConfiguration)
        {
            var paths = new List<string>();

            if (File.Exists(csprojFullPath))
            {
                var xdoc = XDocument.Load(csprojFullPath);
                XNamespace ns = xdoc.Root.Name.Namespace;

                var projectDirectory = Path.GetDirectoryName(csprojFullPath);

                var assemblyName = xdoc.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value;
                var outputType = xdoc.Descendants(ns + "OutputType").FirstOrDefault()?.Value;
                var outputPath = GetOutputPath(xdoc, ns, activeConfiguration);

                if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(outputType) && !string.IsNullOrEmpty(outputPath))
                {
                    var fileExtension = outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase) ? "exe"
                                      : outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? "dll"
                                      : null;

                    if (fileExtension != null)
                    {
                        var normalizedOutputPath = Path.GetFullPath(Path.Combine(projectDirectory, outputPath));
                        var finalOutputPath = Path.Combine(normalizedOutputPath, $"{assemblyName}.{fileExtension}");

                        finalOutputPath = Environment.ExpandEnvironmentVariables(finalOutputPath);
                        paths.Add(finalOutputPath);
                    }
                }
            }

            return paths;
        }

        private static List<string> GetWinExeProjectGuids(string solutionPath)
        {
            var projectGuids = new List<string>();
            string solutionDirectory = Path.GetDirectoryName(solutionPath);

            // 获取解决方案中的所有 .csproj 路径
            foreach (var line in File.ReadLines(solutionPath))
            {
                var match = Regex.Match(line, @"""([^""]+\.csproj)""");
                if (match.Success)
                {
                    var relativeProjectPath = match.Groups[1].Value;
                    var absoluteProjectPath = Path.Combine(solutionDirectory, relativeProjectPath);

                    // 读取 .csproj 文件以查找 <OutputType>WinExe</OutputType> 和 <ProjectGuid>
                    if (File.Exists(absoluteProjectPath))
                    {
                        var xdoc = XDocument.Load(absoluteProjectPath);
                        XNamespace ns = xdoc.Root.Name.Namespace;

                        // 检查 <OutputType> 是否为 WinExe
                        var outputType = xdoc.Descendants(ns + "OutputType").FirstOrDefault()?.Value;
                        if (outputType != null && outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase))
                        {
                            // 提取 <ProjectGuid>
                            var projectGuid = xdoc.Descendants(ns + "ProjectGuid").FirstOrDefault()?.Value;
                            if (!string.IsNullOrEmpty(projectGuid))
                            {
                                projectGuids.Add(projectGuid);
                            }
                        }
                    }
                }
            }

            return projectGuids;
        }



        private static string GetOutputPath(XDocument xdoc, XNamespace ns, string activeConfiguration)
        {
            var outputPath = xdoc.Descendants(ns + "PropertyGroup")
                .Where(pg => pg.Attribute("Condition") != null && pg.Attribute("Condition").Value.Contains(activeConfiguration))
                .Descendants(ns + "OutputPath")
                .FirstOrDefault()?.Value;

            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = xdoc.Descendants(ns + "PropertyGroup")
                    .Where(pg => pg.Attribute("Condition") == null)
                    .Descendants(ns + "OutputPath")
                    .FirstOrDefault()?.Value;
            }

            return outputPath;
        }

        //获取从dll文件夹和活动解决方案配置（Debug或Release）中获取到的dll文件
        private static IEnumerable<string> GetReferencedDllPaths(string solutionDirectory, string activeConfiguration)
        {
            var dllPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var solutionFilePath = Directory.GetFiles(solutionDirectory, "*.sln").FirstOrDefault();

            if (string.IsNullOrEmpty(solutionFilePath))
            {
                throw new FileNotFoundException("Solution file (.sln) not found.");
            }

            // 用于存储输出类型为 Library 的项目生成的 dll 文件名
            var libraryDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 读取 .sln 文件并处理项目
            foreach (var line in File.ReadAllLines(solutionFilePath))
            {
                if (line.StartsWith("Project("))
                {
                    // 获取项目的相对路径
                    var projectPath = line.Split(',')[1].Trim().Trim('"');
                    var fullPath = Path.Combine(Path.GetDirectoryName(solutionFilePath), projectPath);

                    if (File.Exists(fullPath) && fullPath.EndsWith(".csproj"))
                    {
                        var projectDirectory = Path.GetDirectoryName(fullPath);
                        var projectName = Path.GetFileNameWithoutExtension(fullPath);

                        // 读取 .csproj 文件内容
                        var csprojContent = File.ReadAllText(fullPath);
                        if (csprojContent.Contains("<OutputType>Library</OutputType>"))
                        {
                            // 构建项目的输出目录，根据活动配置生成
                            var outputFolderPath = Path.Combine(projectDirectory, "bin", activeConfiguration);
                            var outputDllPath = Path.Combine(outputFolderPath, $"{projectName}.dll");

                            if (File.Exists(outputDllPath))
                            {
                                libraryDlls.Add(Path.GetFileName(outputDllPath));
                            }
                        }
                    }
                }
            }

            // 构建 dll 文件夹和活动配置文件夹路径
            var dllFolderPath = Path.Combine(solutionDirectory, "dll");
            var activeConfigFolderPath = Path.Combine(solutionDirectory, activeConfiguration);

            // 遍历 dll 文件夹中的 .dll 文件（如果文件夹存在）
            if (Directory.Exists(dllFolderPath))
            {
                var dllFiles = Directory.GetFiles(dllFolderPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dllFile in dllFiles)
                {
                    var fileName = Path.GetFileName(dllFile);
                    if (!dllPaths.ContainsKey(fileName) && !libraryDlls.Contains(fileName))
                    {
                        dllPaths[fileName] = dllFile;
                    }
                }
            }

            // 遍历活动配置文件夹中的 .dll 文件（如果文件夹存在）
            if (Directory.Exists(activeConfigFolderPath))
            {
                var activeConfigDllFiles = Directory.GetFiles(activeConfigFolderPath, "*.dll", SearchOption.AllDirectories);
                foreach (var dllFile in activeConfigDllFiles)
                {
                    var fileName = Path.GetFileName(dllFile);
                    // 覆盖 dll 文件夹中的相同文件，优先保存活动配置文件夹中的文件路径
                    if (!libraryDlls.Contains(fileName))
                    {
                        dllPaths[fileName] = dllFile;
                    }
                }
            }

            // 返回去重后的 dll 文件路径列表
            return dllPaths.Values;
        }

        private async Task CleanAndBuildSolutionAsync(DTE2 dte)
        {
            await Task.Run(() =>
            {


                dte.Solution.SolutionBuild.Clean(true);
                dte.Solution.SolutionBuild.Build(true);

                ThreadHelper.JoinableTaskFactory.Run(async () => await LogMessageAsync("正在重新生成解决方案..."));

                while (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
                {
                    System.Threading.Thread.Sleep(100);
                }
            });
        }

        //鸡肋
        private static string IsExistsConfig(string ConfigfilePath)
        {
            if (File.Exists(ConfigfilePath))
            {
                return null;
            }
            else
            {
                string NoExists = "配置文件citedllconfig.json不存在，将在\\Common7\\IDE中创建配置文件...";
                return NoExists;
            }
        }
    }

    internal class ReactorHelper
    {
        private ReactorHelper() { }

        private static ReactorHelper instance = new ReactorHelper();
        public static ReactorHelper Instance => instance;

        private IVsOutputWindowPane outputPane;
        private List<string> exePath = new List<string>();
        private List<string> dllPath = new List<string>();
        private string folder = null;

        public async Task StartAsync(List<string> outputPaths, IVsOutputWindowPane outputPane)
        {
            this.outputPane = outputPane;
            await StartAsync(outputPaths);
        }

        public async Task StartAsync(List<string> outputPaths)
        {
            dllPath.Clear();
            exePath.Clear();

            try
            {
                dllPath.AddRange(outputPaths.Where(path => path.EndsWith(".dll")));
                exePath.AddRange(outputPaths.Where(path => path.EndsWith(".exe")));

                folder = exePath.FirstOrDefault() != null ? Path.GetDirectoryName(exePath.First()) : null;

                await WriteLogAsync("EXE 文件：");
                foreach (string path in exePath)
                {
                    await WriteLogAsync(path);
                }

                await WriteLogAsync("DLL 文件：");
                foreach (string path in dllPath)
                {
                    await WriteLogAsync(path);
                }

                await WriteLogAsync("EXE 和 DLL 所在的文件夹路径：");
                await WriteLogAsync(folder);

                await WriteLogAsync("启动项目录");
                await WriteLogAsync(Application.StartupPath);

                Assembly asm = Assembly.LoadFile(Application.StartupPath + "\\dotNET_Reactor.exe");

                BeginMainForm(asm);

                Type t2 = asm.GetType("tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw");
                string[] d = new string[0];
                Form m = (Form)Activator.CreateInstance(t2, new object[] { d });
                m.Load += async (sender, e) => await M_LoadAsync(sender, e);

                MethodInfo minfoClosing = t2.GetMethod("veG6FEx6PmE", BindingFlags.Instance | BindingFlags.NonPublic);
                FormClosingEventHandler frmClosingEventHandler = Delegate.CreateDelegate(typeof(FormClosingEventHandler), m, minfoClosing) as FormClosingEventHandler;
                m.FormClosing -= frmClosingEventHandler;
                m.ShowDialog();
            }
            catch (Exception ex)
            {
                await WriteLogAsync("错误：" + ex.Message + ex.StackTrace);
            }
        }

        private async Task WriteLogAsync(string msg)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (outputPane != null)
            {
                outputPane.OutputString($"{msg}\n");
                outputPane.Activate();
            }

            System.Diagnostics.Debug.WriteLine(msg);
            Console.WriteLine(msg);
            LogHelper.LogInfo(msg);
        }

        private void BeginMainForm(Assembly asm)
        {
            Environment.ExitCode = 0;
            RSACryptoServiceProvider.UseMachineKeyStore = true;
            bool flag = true;

            SetStaticFieldValue(asm, "tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw", "VBs68HDa8QB",
                RunStaticMethod(asm, "tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw", "CLk6FZTWYjT", null));
            SetStaticFieldValue(asm, "tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw", "VBs68HDa8QB", flag);
            SetStaticFieldValue(asm, "tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw", "fAA68zfLC7X", flag);

            SetStaticFieldValue(asm, "BQk3VxME5qkanBXTjkD.ARIjZHMpMVkoQ2M0SA0", "QwgqvCejwQk", flag);

            RunStaticMethod(asm, "OI6Zx4TbIpeYOpQHHWv.OlgbTtTy2stDRLaWM37", "Uvq62dAwXMW", new object[] { flag });
            FieldInfo finfo = asm.GetType("OI6Zx4TbIpeYOpQHHWv.OlgbTtTy2stDRLaWM37").GetField("HaD6i1I9vvb", BindingFlags.NonPublic | BindingFlags.Static);
            SortedList sl = (SortedList)finfo.GetValue(null);

            sl["REG_NAME"] = "PC-RET";
            sl["FIRSTNAME"] = "PC-RET";
            sl["LASTNAME"] = "PC-RET";
            sl["COMPANY"] = "PC-RET";

            SetStaticFieldValue(asm, "BQk3VxME5qkanBXTjkD.ARIjZHMpMVkoQ2M0SA0", "QwgqvCejwQk", flag);
            SetStaticFieldValue(asm, "BQk3VxME5qkanBXTjkD.ARIjZHMpMVkoQ2M0SA0", "xiuqvLYK9bi", " Full Version [PC-RET] ");
            RunStaticMethod(asm, "BQk3VxME5qkanBXTjkD.ARIjZHMpMVkoQ2M0SA0", "AaIqYQaDAH3", null);
            SetStaticFieldValue(asm, "BQk3VxME5qkanBXTjkD.ARIjZHMpMVkoQ2M0SA0", "WrEq3RXXElD", Application.ExecutablePath);
        }

        private void SetStaticFieldValue(Assembly asm, string typeName, string fieldName, object value)
        {
            Type t = asm.GetType(typeName);
            if (t != null)
            {
                FieldInfo finfo = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                finfo.SetValue(null, value);
            }
        }

        private object RunStaticMethod(Assembly asm, string typeName, string methodName, object[] paras)
        {
            object o = null;
            Type t = asm.GetType(typeName);
            if (t != null)
            {
                MethodInfo finfo = t.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                o = finfo.Invoke(null, paras);
            }
            return o;
        }

        private async Task M_LoadAsync(object sender, EventArgs e)
        {
            try
            {
                Type t2 = sender.GetType();
                Form m = sender as Form;

                SetExeName(t2, m);

                AddDlls(t2, m);

                CheckParameter(t2, m);

                DoProtect(t2, m);

                bool exeOk = await WaitOK(t2, m);

                if (exeOk)
                {
                    CoverExe(t2, m);
                    await WriteLogAsync("混淆完成！");
                }
                else
                {
                    await WriteLogAsync("执行超时！");
                }

                m.Close();
            }
            catch (Exception ex)
            {
                await WriteLogAsync(ex.Message);
            }
        }

        private void SetExeName(Type t2, Form m)
        {
            FieldInfo finfoS = t2.GetField("GWb68qY3BI7", BindingFlags.NonPublic | BindingFlags.Static);
            if (finfoS != null)
            {
                Type t = finfoS.FieldType;
                MethodInfo finfo = t.GetMethod("fKCqv4usqbD", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                finfo.Invoke(finfoS.GetValue(m), new object[] { exePath[0] });
            }

            ExeMethod(t2, m, "Fjn67kKclon", null);
            ExeMethod(t2, m, "a0x67DIEWvF", new object[] { exePath[0], false });
            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("EXE文件加入完成！"));
            ExeMethod(t2, m, "Pn16mNwCxI9", new object[] { null, null });
        }

        private void ExeMethod(Type t2, Form m, string methodName, object[] paras)
        {
            MethodInfo finfo = t2.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            finfo.Invoke(m, paras);
        }

        private void AddDlls(Type t2, Form m)
        {
            foreach (string path in dllPath)
            {
                string dllName = path;
                MethodInfo minfo = t2.GetMethod("jYK6Fqkjbaw", BindingFlags.Instance | BindingFlags.NonPublic);
                minfo.Invoke(m, new object[] { dllName });
                ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("DLL文件加入完成！"));
            }
        }

        private void CheckParameter(Type t2, Form m)
        {
            string[] needCheck = {
                        "BhG6TTKjMLT",
                        "sfv6E8JGK5Y",
                        "Lqf6BXXtthr",
                        "RYk6EBh7eJO"
                    };
            for (int i = 0; i < needCheck.Length; i++)
            {
                CheckItems(t2, m, needCheck[i], true);
            }

            string[] noneCheck = {
                    "eMa6TENDesi",
                    "ocZ6BxB50JI",
                    "nQ56EcNSIok"
                    };
            for (int i = 0; i < noneCheck.Length; i++)
            {
                CheckItems(t2, m, noneCheck[i], false);
            }
            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("混淆选项设置完成！"));
            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("混淆中..."));
        }

        private void CheckItems(Type t, object instance, string fieldName, bool ifCheck)
        {
            FieldInfo finfo = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            CheckBox checkBox1 = (CheckBox)finfo.GetValue(instance);
            checkBox1.Checked = ifCheck;
        }

        private void DoProtect(Type t2, Form m)
        {
            MethodInfo minfo = t2.GetMethod("ph36mdvTtnm", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            minfo.Invoke(m, new object[] { null, null });
        }

        private async Task<bool> WaitOK(Type t2, Form m)
        {
            bool result = false;

            // 进度条
            FieldInfo finfo = t2.GetField("aGD6BV4JhFs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            if (finfo != null)
            {
                PropertyInfo pInner = finfo.FieldType.GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                if (pInner != null)
                {
                    int previousValue = -1;
                    int stableCount = 0;
                    const int maxStableCount = 5; // 最大稳定次数，表示卡住的阈值

                    for (int i = 0; i < 500; i++)
                    {
                        object o = pInner.GetValue(finfo.GetValue(m), null);

                        if (o != null && o.ToString() == "100")
                        {
                            result = true;
                            break;
                        }

                        if (o != null && int.TryParse(o.ToString(), out int currentValue))
                        {
                            // 检查进度是否卡住
                            if (currentValue == previousValue)
                            {
                                stableCount++;
                                if (stableCount >= maxStableCount)
                                {
                                    // 获取TextBox内容
                                    string textBoxContent = GetErrorDetails(t2, m);
                                    throw new Exception($"进度条卡住了: {textBoxContent ?? "无内容"}");
                                }
                            }
                            else
                            {
                                stableCount = 0;
                            }
                            previousValue = currentValue;
                        }
                        else
                        {
                            stableCount = 0;
                        }

                        await Task.Delay(200);
                    }
                }
            }

            return result;
        }

        delegate string GetValueDele(PropertyInfo pinfo, object o);
        string GetValue(PropertyInfo pinfo, object o)
        {
            return ((Control.ControlAccessibleObject)pinfo.GetValue(o, null)).Value;
        }

        // 获取TextBox内容的方法
        private string GetErrorDetails(Type t2, object instance)
        {
            try
            {
                FieldInfo fieldInfo = t2.GetField("jjH6EaifrLl", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (fieldInfo != null)
                {
                    object textBox = fieldInfo.GetValue(instance);
                    if (textBox != null)
                    {
                        PropertyInfo linesProperty = textBox.GetType().GetProperty("AccessibilityObject", BindingFlags.Public | BindingFlags.Instance);

                        GetValueDele gg = GetValue;
                        string text = (string)((Form)instance).Invoke(gg, new object[] { linesProperty, textBox });

                        string base64String = text.Substring(text.IndexOf("#") + 1);
                        int index = base64String.LastIndexOf("]");
                        base64String = base64String.Substring(0, index);
                        try
                        {
                            return Encoding.Unicode.GetString(Convert.FromBase64String(base64String));
                            //return Encoding.Unicode.GetString(Convert.FromBase64String(otRyrsTdurv5ecje7rT1.Lines[0].Substring(otRyrsTdurv5ecje7rT1.Lines[0].IndexOf("#") + 1)));

                        }
                        catch (FormatException)
                        {
                            // 处理无效的Base64字符串
                            return "无效的错误信息格式";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("" + ex.Message);
            }
            return null; // 返回null如果未能获取到错误信息
        }


        private void CoverExe(Type t2, Form m)
        {
            foreach (string path in dllPath)
            {
                File.Delete(path);
            }

            foreach (string path in exePath)
            {
                File.Delete(path);
            }
            foreach (string directory in Directory.GetDirectories(folder))
            {
                if (directory.Contains("_Secure"))
                {
                    string[] files = Directory.GetFiles(directory);

                    foreach (string file in files)
                    {
                        if (file.EndsWith(".exe") || file.EndsWith(".dll"))
                        {
                            string destinationFilePath = Path.Combine(folder, Path.GetFileName(file));
                            File.Move(file, destinationFilePath);
                        }
                    }

                    Directory.Delete(directory, true);
                }
            }
        }
    }
}