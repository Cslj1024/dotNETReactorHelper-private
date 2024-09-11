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

namespace dotNETReactorHelper
{
    internal sealed class ReactorHelperCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("55c8d9db-3e7d-44c3-81b5-a74f564a3ed5");
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

                    // 获取当前活动配置是 "Debug" 还是 "Release"
                    string activeConfiguration = GetActiveConfiguration(dte);
                    await LogMessageAsync($"当前活动配置: {activeConfiguration}");

                    var csprojPaths = GetCsprojPaths(solutionPath, solutionDirectory);

                    // 自动混淆 .exe 和 .dll 文件
                    var exeAndDllPaths = csprojPaths.SelectMany(path => GetExeAndDllPaths(path, activeConfiguration)).ToList();
                    outputPaths.AddRange(exeAndDllPaths);

                    // 从 .csproj 文件中获取引用的 .dll 文件路径
                    var referencedDllPaths = csprojPaths.SelectMany(path => GetReferencedDllPaths(path, activeConfiguration)).ToList();

                    // 打开 DisPlayForm 让用户选择要混淆的 DLL
                    using (var displayForm = new DisPlayForm(referencedDllPaths))
                    {
                        var result = displayForm.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            outputPaths.AddRange(displayForm.SelectedDllPaths);
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

        private static IEnumerable<string> GetReferencedDllPaths(string csprojFullPath, string activeConfiguration)
        {
            var dllPaths = new List<string>();

            if (File.Exists(csprojFullPath))
            {
                var xdoc = XDocument.Load(csprojFullPath);
                XNamespace ns = xdoc.Root.Name.Namespace;

                var projectDirectory = Path.GetDirectoryName(csprojFullPath);

                var references = xdoc.Descendants(ns + "Reference")
                                     .Where(r => r.Element(ns + "HintPath") != null)
                                     .Select(r => Path.GetFullPath(Path.Combine(projectDirectory, r.Element(ns + "HintPath").Value)));

                dllPaths.AddRange(references);
            }

            return dllPaths;
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

                SetExeName2(t2, m);

                addDlls(t2, m);

                checkParameter(t2, m);

                doProtect(t2, m);

                bool exeOk = await waitOK(t2, m);

                if (exeOk)
                {
                    coverExe(t2, m);
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

        private void SetExeName2(Type t2, Form m)
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
            ExeMethod(t2, m, "Pn16mNwCxI9", new object[] { null, null });
        }

        private void ExeMethod(Type t2, Form m, string methodName, object[] paras)
        {
            MethodInfo finfo = t2.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            finfo.Invoke(m, paras);
        }

        private void addDlls(Type t2, Form m)
        {
            foreach (string path in dllPath)
            {
                string dllName = path;
                MethodInfo minfo = t2.GetMethod("jYK6Fqkjbaw", BindingFlags.Instance | BindingFlags.NonPublic);
                minfo.Invoke(m, new object[] { dllName });
                ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("DLL文件加入完成！"));
            }
        }

        private void checkParameter(Type t2,

 Form m)
        {
            string[] needCheck = {
                        "BhG6TTKjMLT",
                        "sfv6E8JGK5Y",
                        "Lqf6BXXtthr",
                        "RYk6EBh7eJO"
                    };
            for (int i = 0; i < needCheck.Length; i++)
            {
                checkItems(t2, m, needCheck[i], true);
            }

            string[] noneCheck = {
                    "eMa6TENDesi",
                    "ocZ6BxB50JI",
                    "nQ56EcNSIok"
                    };
            for (int i = 0; i < noneCheck.Length; i++)
            {
                checkItems(t2, m, noneCheck[i], false);
            }
            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("混淆选项设置完成！"));
        }

        private void checkItems(Type t, object instance, string fieldName, bool ifCheck)
        {
            FieldInfo finfo = t.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            CheckBox checkBox1 = (CheckBox)finfo.GetValue(instance);
            checkBox1.Checked = ifCheck;
        }

        private void doProtect(Type t2, Form m)
        {
            MethodInfo minfo = t2.GetMethod("ph36mdvTtnm", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            minfo.Invoke(m, new object[] { null, null });
            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("开始混淆！"));
        }

        private async Task<bool> waitOK(Type t2, Form m)
        {
            bool result = false;
            FieldInfo finfo = t2.GetField("aGD6BV4JhFs", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (finfo != null)
            {
                PropertyInfo pInner = finfo.FieldType.GetProperty("Value", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (pInner != null)
                {
                    for (int i = 0; i < 500; i++)
                    {
                        object o = pInner.GetValue(finfo.GetValue(m), null);
                        if (o != null && o.ToString() == "100")
                        {
                            result = true;
                            await WriteLogAsync("混淆代码到新文件完成！");
                            break;
                        }
                        else
                        {
                            await WriteLogAsync("混淆代码到新文件完成 " + o.ToString() + " % ！");
                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
            }
            return result;
        }

        private void coverExe(Type t2, Form m)
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

            ThreadHelper.JoinableTaskFactory.Run(async () => await WriteLogAsync("混淆文件覆盖原文件完成！"));
            MessageBox.Show("混淆文件完成");
        }
    }
}