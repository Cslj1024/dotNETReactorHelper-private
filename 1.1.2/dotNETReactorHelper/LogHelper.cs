﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace ReactorHelper
{
    //该类由BS风华改编自网上一个类。实现了多线程下面安全记录日志的功能：
    //使用方法：
    //Tools.Log.LogHelper.LogFlag = true; //开启记录,默认已经打开
    //Tools.Log.LogHelper.LogInfo("==========日志记录内容 Info====");
    //Tools.Log.LogHelper.LogWarn("==========日志记录内容 Warn====");
    //Tools.Log.LogHelper.LogError("==========日志记录内容 Error====");
    //Tools.Log.LogHelper.LogFlag = false;//停止记录
                                        
    //Tools.Log.LogHelper.ExitThread();// 退出日志记录线程，一般在程序退出时候调用

    /// <summary>
    /// 多线程安全Log记录工具20180314
    /// </summary>
    public class LogHelper
    {
        //为了使用DBGView进行在线调试：
        //System.Diagnostics.Debug.WriteLine("Debug模式可见")
        //System.Diagnostics.Trace.WriteLine("Debug、Release都可见");

        private static Thread WriteThread;
        private static readonly Queue<string> MsgQueue;

        private static readonly string FilePath;

        private static Boolean autoResetEventFlag = false;
        private static AutoResetEvent aEvent = new AutoResetEvent(false);
        private static bool flag = true;
        public static bool LogFlag = true;

        static LogHelper()
        {
            FilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "log\\";
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
            WriteThread = new Thread(WriteMsg);
            MsgQueue = new Queue<string>();
            WriteThread.Start();
        }

        public static void LogInfo(string msg)
        {
            Monitor.Enter(MsgQueue);
            MsgQueue.Enqueue(string.Format("{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"), "Info", msg));
            Monitor.Exit(MsgQueue);
            if (autoResetEventFlag)
            {
                aEvent.Set();
            }
        }
        public static void LogError(string msg)
        {
            Monitor.Enter(MsgQueue);
            MsgQueue.Enqueue(string.Format("{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"), "Error", msg));
            Monitor.Exit(MsgQueue);
            if (autoResetEventFlag)
            {
                aEvent.Set();
            }
        }
        public static void LogWarn(string msg)
        {
            Monitor.Enter(MsgQueue);
            MsgQueue.Enqueue(string.Format("{0} {1} {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:sss"), "Warn", msg));
            Monitor.Exit(MsgQueue);
            if (autoResetEventFlag)
            {
                aEvent.Set();
            }
        }

        /// <summary>
        /// ExitThread是退出日志记录线程的方法，一旦退出，无法开启，一般在程序关闭时执行
        /// </summary>
        public static void ExitThread()
        {
            flag = false;
            aEvent.Set();//恢复线程执行
        }
        private static void WriteMsg()
        {
            while (flag)
            {
                //进行记录
                if (LogFlag)
                {
                    autoResetEventFlag = false;
                    if (!Directory.Exists(FilePath))
                    {
                        Directory.CreateDirectory(FilePath);
                    }
                    string fileName = FilePath + DateTime.Now.ToString("yyyy-MM-dd") + "-Dic.log";
                    var logStreamWriter = new StreamWriter(fileName, true);
                    while (MsgQueue.Count > 0)
                    {
                        Monitor.Enter(MsgQueue);
                        string msg = MsgQueue.Dequeue();
                        Monitor.Exit(MsgQueue);
                        logStreamWriter.WriteLine(msg);
                        if (GetFileSize(fileName) > 1024 * 5)
                        {
                            logStreamWriter.Flush();
                            logStreamWriter.Close();
                            CopyToBak(fileName);
                            logStreamWriter = new StreamWriter(fileName, false);
                            logStreamWriter.Write("");
                            logStreamWriter.Flush();
                            logStreamWriter.Close();
                            logStreamWriter = new StreamWriter(fileName, true);
                        }
                        //下面用于DbgView.exe工具进行在线调试
                        //System.Diagnostics.Debug.WriteLine("BS_Debug:" + msg);
                        //System.Diagnostics.Trace.WriteLine("BS_Release:" + msg);
                    }
                    logStreamWriter.Flush();
                    logStreamWriter.Close();
                    autoResetEventFlag = true;
                    aEvent.WaitOne();
                }
                else
                {
                    autoResetEventFlag = true;
                    aEvent.WaitOne();
                }
            }
        }
        private static long GetFileSize(string fileName)
        {
            long strRe = 0;
            if (File.Exists(fileName))
            {
                var myFs = new FileInfo(fileName);
                strRe = myFs.Length / 1024;
                //Console.WriteLine(strRe);
            }
            return strRe;
        }
        private static void CopyToBak(string sFileName)
        {
            int fileCount = 0;
            string sBakName = "";
            do
            {
                fileCount++;
                sBakName = sFileName + "." + fileCount + ".BAK";
            }
            while (File.Exists(sBakName));
            File.Copy(sFileName, sBakName);
        }
    }
}
