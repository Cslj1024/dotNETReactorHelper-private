using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using VSLangProj;
using Microsoft.Build.Framework;
using System.Diagnostics;
using ReactorHelper;

namespace dotNETReactorHelper
{
    internal sealed class ReactorHelperCommand1
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("346b15aa-d711-45f2-9483-48ad9a7b2049");
        private readonly AsyncPackage package;

        // 全局变量定义
        public static List<string> outputPaths = new List<string>();

        private ReactorHelperCommand1(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            if (commandService != null)
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
            }
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            OleMenuCommandService commandService = await package.GetServiceAsync((typeof(IMenuCommandService))) as OleMenuCommandService;
            new ReactorHelperCommand1(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            outputPaths.Clear();

            ThreadHelper.ThrowIfNotOnUIThread();
            var dte = (DTE2)ServiceProvider.GetService(typeof(DTE));
            if (dte != null)
            {


                var solution = dte.Solution;
                string solutionPath = solution.FullName;
                string solutionDirectory = Path.GetDirectoryName(solutionPath);

                var csprojPaths = GetCsprojPaths(solutionPath, solutionDirectory);
                outputPaths.AddRange(csprojPaths.SelectMany(path => GetOutputPaths(path)));

                //ReactorHelper.Instance.Start();

                // 调试信息
                var debugInfo = $"解决方案路径: {solutionPath}\n解决方案目录: {solutionDirectory}\n\nCSProj路径:\n{string.Join("\n", csprojPaths)}\n\n输出文件路径:\n{string.Join("\n", outputPaths)}";
                System.Diagnostics.Debug.WriteLine(debugInfo);
                Console.WriteLine(debugInfo);

                // 显示 csprojFullPath 信息
                foreach (var csprojFullPath in csprojPaths)
                {
                    System.Diagnostics.Debug.WriteLine($"CSProj Full Path: {csprojFullPath}");
                    Console.WriteLine($"CSProj Full Path: {csprojFullPath}");
                }

                // 重新生成解决方案
                dte.Solution.SolutionBuild.Clean(true);
                dte.Solution.SolutionBuild.Build(true);

                System.Diagnostics.Debug.WriteLine("正在重新生成解决方案...");
                Console.WriteLine("正在重新生成解决方案...");

                // 等待重新生成解决方案完成
                while (dte.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
                {
                    System.Threading.Thread.Sleep(100);
                }

                if (dte.Solution.SolutionBuild.LastBuildInfo == 0)
                {
                    System.Diagnostics.Debug.WriteLine(dte.Solution.SolutionBuild);
                    System.Diagnostics.Debug.WriteLine("解决方案重新生成完成。");
                    Console.WriteLine("解决方案重新生成完成。");  

                    //VsShellUtilities.ShowMessageBox(
                    //    this.package,
                    //    debugInfo,
                    //    "解决方案信息",
                    //    OLEMSGICON.OLEMSGICON_INFO,
                    //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);

                    // 调用 ReactorHelper 的 Start 方法
                    System.Diagnostics.Debug.WriteLine("开始执行代码混淆！");
                    Console.WriteLine("开始执行代码混淆！");

                    //ProcessStartInfo pinfo = new ProcessStartInfo();
                    //pinfo.FileName = @"D:\Reactor\ReactorHelper.exe";

                    //string arges = "";

                    //foreach(string p in outputPaths)
                    //{
                    //    arges += p + " ";
                    //}
                    //pinfo.Arguments = arges;

                    //System.Diagnostics.Process.Start(pinfo);

                    ReactorHelper.Instance.Start(outputPaths);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("重新生成解决方案失败。");
                    Console.WriteLine("重新生成解决方案失败。");
                    //VsShellUtilities.ShowMessageBox(
                    //    this.package,
                    //    "重新生成解决方案失败，请检查输出窗口以了解更多信息。",
                    //    "构建失败",
                    //    OLEMSGICON.OLEMSGICON_CRITICAL,
                    //    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    //    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
                }
            }
        }

        private IServiceProvider ServiceProvider => this.package;

        private static string[] GetCsprojPaths(string solutionPath, string solutionDirectory)
        {
            var csprojPaths = new System.Collections.Generic.List<string>();

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

        private static System.Collections.Generic.IEnumerable<string> GetOutputPaths(string csprojFullPath)
        {
            var outputPaths = new System.Collections.Generic.List<string>();

            if (File.Exists(csprojFullPath))
            {
                var xdoc = XDocument.Load(csprojFullPath);
                XNamespace ns = xdoc.Root.Name.Namespace;

                var assemblyName = xdoc.Descendants(ns + "AssemblyName").FirstOrDefault()?.Value;
                var outputType = xdoc.Descendants(ns + "OutputType").FirstOrDefault()?.Value;
                var outputPath = GetOutputPath(xdoc, ns);

                // Debugging information
                System.Diagnostics.Debug.WriteLine($"Parsing: {csprojFullPath}");
                System.Diagnostics.Debug.WriteLine($"AssemblyName: {assemblyName}");
                System.Diagnostics.Debug.WriteLine($"OutputType: {outputType}");
                System.Diagnostics.Debug.WriteLine($"OutputPath: {outputPath}");

                if (!string.IsNullOrEmpty(assemblyName) && !string.IsNullOrEmpty(outputType) && !string.IsNullOrEmpty(outputPath))
                {
                    var fileExtension = outputType.Equals("WinExe", StringComparison.OrdinalIgnoreCase) ? "exe"
                                      : outputType.Equals("Library", StringComparison.OrdinalIgnoreCase) ? "dll"
                                      : null;

                    if (fileExtension != null)
                    {
                        var projectDirectory = Path.GetDirectoryName(csprojFullPath);
                        var normalizedOutputPath = Path.GetFullPath(Path.Combine(projectDirectory, outputPath));
                        var finalOutputPath = Path.Combine(normalizedOutputPath, $"{assemblyName}.{fileExtension}");

                        // 处理路径中的环境变量
                        finalOutputPath = Environment.ExpandEnvironmentVariables(finalOutputPath);

                        outputPaths.Add(finalOutputPath);
                    }
                }
            }

            return outputPaths;
        }

        private static string GetOutputPath(XDocument xdoc, XNamespace ns)
        {
            // 处理 <OutputPath> 节点的条件属性
            var outputPath = xdoc.Descendants(ns + "PropertyGroup")
                .Where(pg => pg.Attribute("Condition") != null && pg.Attribute("Condition").Value.Contains("Debug|AnyCPU"))
                .Descendants(ns + "OutputPath")
                .FirstOrDefault()?.Value;

            // 如果未找到带有条件的 <OutputPath>，则查找没有条件的 <OutputPath>
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = xdoc.Descendants(ns + "PropertyGroup")
                    .Where(pg => pg.Attribute("Condition") == null)
                    .Descendants(ns + "OutputPath")
                    .FirstOrDefault()?.Value;
            }

            return outputPath;
        }
    }

    
        internal class ReactorHelper
        {
            private ReactorHelper() { }

            private static ReactorHelper instance = new ReactorHelper();
            public static ReactorHelper Instance
            {
                get { return instance; }
            }

            Form m;
            Type t2;
            List<string> exePath = new List<string>();
            List<string> dllPath = new List<string>();
            //exe和dll的文件夹的路径
            String folder = null;

            private void WriteLog(string msg)
            {
                System.Diagnostics.Debug.WriteLine(msg);
                Console.WriteLine(msg);
                LogHelper.LogInfo(msg);
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

            public void Start(List<string> outputPaths)
            {
                exePath.Clear();
                dllPath.Clear();
                try
                {
                    try
                    {
                        // 使用 outputPaths 代替读取 Config.txt
                        foreach (string line in outputPaths)
                        {
                            //// 去除两侧的双引号
                            //if (line.StartsWith("\"") && line.EndsWith("\""))
                            //{
                            //    line = line.Substring(1, line.Length - 2);
                            //}

                            //根据后缀名对行进行分类
                            if (line.EndsWith(".exe"))
                            {
                                exePath.Add(line);
                                //获取exe所在的文件夹路径
                                folder = Path.GetDirectoryName(line);
                                WriteLog("exe所在文件夹");
                                WriteLog(folder);
                            }
                            else if (line.EndsWith(".dll"))
                            {
                                dllPath.Add(line);
                                //获取dll所在的文件夹路径
                                folder = Path.GetDirectoryName(line);
                                WriteLog("dll所在文件夹");
                                WriteLog(folder);
                            }
                        }

                        WriteLog("EXE 文件：");
                        foreach (string path in exePath)
                        {
                            WriteLog(path);
                        }

                        WriteLog("DLL 文件：");
                        foreach (string path in dllPath)
                        {
                            WriteLog(path);
                        }

                        //输出exe和dll所在的文件夹路径
                        WriteLog("EXE 和 DLL 所在的文件夹路径：");
                        WriteLog(folder);
                    }
                    catch (Exception e)
                    {
                        WriteLog("该文件无法读取:");
                        WriteLog(e.Message);
                    }

                    WriteLog("启动项目录");
                    WriteLog(Application.StartupPath);

                    Assembly asm = Assembly.LoadFile(Application.StartupPath + "\\dotNET_Reactor.exe");

                    BeginMainForm(asm);

                    t2 = asm.GetType("tL15cJBRNr8twtD1dM9.CDYft5BYRvIPGxfQ6jw");
                    string[] d = new string[0];
                    m = (Form)Activator.CreateInstance(t2, new object[] { d });
                    m.Load += M_Load;

                    //Application.Run(m);
                    MethodInfo minfoClosing = t2.GetMethod("veG6FEx6PmE", BindingFlags.Instance | BindingFlags.NonPublic);
                    FormClosingEventHandler frmClosingEventHandler = Delegate.CreateDelegate(typeof(FormClosingEventHandler), m, minfoClosing) as FormClosingEventHandler;
                    m.FormClosing -= frmClosingEventHandler;
                    m.ShowDialog();
                    //Application.Run(m);
                }
                catch (Exception ex)
                {
                    WriteLog("错误： " + ex.Message + ex.StackTrace);
                }
            }

            private async void M_Load(object sender, EventArgs e)
            {
                try
                {
                    //Task.Run(() =>
                    //{
                    //设置exe文件名称
                    //setExeName(t2, m); 
                    SetExeName2(t2, m);

                    //添加dll列表
                    addDlls(t2, m);

                    //选择保护参数
                    checkParameter(t2, m);

                    //执行保护
                    doProtect(t2, m);

                    //等待执行完成
                    bool exeOk = await waitOK(t2, m);
                    //bool exeOk = waitOK(t2, m);

                    if (exeOk)
                    {
                        //覆盖过程
                        coverExe(t2, m);
                        //拷贝文件
                        //copyBack();
                        WriteLog("混淆完成！");
                    }
                    else
                    {
                        WriteLog("执行超时！");
                    }

                    m.Close();
                }
                catch (Exception ex)
                {
                    WriteLog(ex.Message);
                }
            }

            private void setExeName(Type t2, Form m)
            {
                //exe名称
                //ComboBox PfM6TVBAb8u
                FieldInfo finfo = t2.GetField("PfM6TVBAb8u", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                ComboBox combo = (ComboBox)finfo.GetValue(m);
                //combo.Text = Application.StartupPath + "\\W1.exe";
                combo.Text = exePath[0];
                WriteLog("EXE文件加入完成！");
            }

            private void ExeMethod(Type t2, Form m, string methodName, object[] paras)
            {
                MethodInfo finfo = t2.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                finfo.Invoke(m, paras);
            }

            private void SetExeName2(Type t2, Form m)
            {
                //CDYft5BYRvIPGxfQ6jw.GWb68qY3BI7.fKCqv4usqbD(exePath[0]);
                FieldInfo finfoS = t2.GetField("GWb68qY3BI7", BindingFlags.NonPublic | BindingFlags.Static);
                if (finfoS != null)
                {
                    Type t = finfoS.FieldType;
                    MethodInfo finfo = t.GetMethod("fKCqv4usqbD", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                    finfo.Invoke(finfoS.GetValue(m), new object[] { exePath[0] });
                }

                //this.Fjn67kKclon();
                ExeMethod(t2, m, "Fjn67kKclon", null);
                //this.a0x67DIEWvF(exePath[0], false);
                ExeMethod(t2, m, "a0x67DIEWvF", new object[] { exePath[0], false });
                //this.Pn16mNwCxI9(null, null);
                ExeMethod(t2, m, "Pn16mNwCxI9", new object[] { null, null });
            }

            private void addDlls(Type t2, Form m)
            {
                //FieldInfo finfo = t2.GetField("tSF6TPADs32", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                //ListView lv = (ListView)finfo.GetValue(m);
                //MethodInfo minfo = t2.GetMethod("Hnv67oR7hkg", BindingFlags.Instance | BindingFlags.Public); 
                //string text = (string)minfo.Invoke(m, new object[] { dllName, lv.Columns[0].Width - 60, lv.Font });
                //ListViewItem item = new ListViewItem(text);
                //lv.Items.Add(item);
                foreach (string path in dllPath)
                {
                    //string dllName = Application.StartupPath + "\\C1.dll";
                    string dllName = path;
                    MethodInfo minfo = t2.GetMethod("jYK6Fqkjbaw", BindingFlags.Instance | BindingFlags.NonPublic);
                    minfo.Invoke(m, new object[] { dllName });
                    WriteLog("DLL文件加入完成！");
                }
            }

            ////选择保护参数
            //CheckBox BhG6TTKjMLT;//NECROBIT
            //CheckBox sfv6E8JGK5Y;//Anti ILDASM
            //CheckBox Lqf6BXXtthr;//anti Tampering
            //CheckBox RYk6EBh7eJO;//string encryption

            //CheckBox eMa6TENDesi;//obfuscation
            //CheckBox ocZ6BxB50JI;//control flow obfuscation
            //CheckBox nQ56EcNSIok;//compress & encrypt resources 
            private void checkParameter(Type t2, Form m)
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
                WriteLog("混淆选项设置完成！");
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
                WriteLog("开始混淆！");
            }

            /// <summary>
            /// 等待完成
            /// </summary>
            /// <param name = "t2" ></ param >
            /// < param name="m"></param>
            /// <returns></returns>
            private Task<bool> waitOK(Type t2, Form m)
            {
                bool result = false;
                //进度条
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
                                WriteLog("混淆代码到新文件完成！");
                                break;
                            }
                            else
                            {
                                WriteLog("混淆代码到新文件完成 " + o.ToString() + " % ！");
                                //Thread.Sleep(200);
                                System.Threading.Thread.Sleep(1000);
                            }
                        }
                    }
                }
                //bool result = true;
                //System.Threading.Thread.Sleep(30000);
                return Task.FromResult(result);
            }


            private void coverExe(Type t2, Form m)
            {
                // 删除 dllPath 和 exePath 中的文件
                foreach (string path in dllPath)
                {
                    File.Delete(path);
                }

                foreach (string path in exePath)
                {
                    File.Delete(path);
                }
                // 遍历指定文件夹的内容
                foreach (string directory in Directory.GetDirectories(folder))
                {
                    // 检查文件夹名是否包含 "_Secure"
                    if (directory.Contains("_Secure"))
                    {
                        // 获取文件夹中的文件
                        string[] files = Directory.GetFiles(directory);

                        // 将文件转移到指定文件夹
                        foreach (string file in files)
                        {
                            if (file.EndsWith(".exe") || file.EndsWith(".dll"))
                            {
                                string destinationFilePath = Path.Combine(folder, Path.GetFileName(file));
                                File.Move(file, destinationFilePath);
                            }
                        }

                        //删除文件夹
                        Directory.Delete(directory, true);
                    }
                }

                WriteLog("混淆文件覆盖原文件完成！");
                MessageBox.Show("混淆文件完成");

                //Environment.Exit(0);
            }
        }
    }

