﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using NewLife.Log;
#if NET4
using System.Linq;
#else
using NewLife.Linq;
#endif

namespace XAgent
{
    /// <summary>
    /// 服务控制器
    /// </summary>
    public class ServiceControl
    {
        #region 服务安装和启动
        /// <summary>安装、卸载 服务</summary>
        /// <param name="isinstall">是否安装</param>
        /// <param name="exeName">Exe名称</param>
        /// <param name="displayName">显示名</param>
        /// <param name="description">描述</param>
        /// <param name="dir">目录</param>
        public static void Install(Boolean isinstall, String exeName = "XAgent", String displayName = "XAgent服务代理", String description = "XAgent服务代理", String dir = "")
        {
            var name = exeName;
            if (dir.IsNullOrWhiteSpace()) dir = AppDomain.CurrentDomain.BaseDirectory;

            WriteLine("在win7/win2008及更高系统中，可能需要管理员权限执行才能安装/卸载服务。");
            if (isinstall)
            {
                RunSC("create " + name + " BinPath= \"" + Path.Combine(dir, exeName) + " -s\" start= auto DisplayName= \"" + displayName + "\"");
                RunSC("description " + name + " \"" + description + "\"");
            }
            else
            {
                ControlService(false);

                RunSC("Delete " + name);
            }
        }

        /// <summary>启动、停止 服务</summary>
        /// <param name="isstart"></param>
        /// <param name="serviceName"></param>
        public static void ControlService(Boolean isstart, String serviceName = "XAgent")
        {
            if (isstart)
                RunCmd("net start " + serviceName, false, true);
            else
                RunCmd("net stop " + serviceName, false, true);
        }

        /// <summary>执行一个命令</summary>
        /// <param name="cmd"></param>
        /// <param name="showWindow"></param>
        /// <param name="waitForExit"></param>
        protected static void RunCmd(String cmd, Boolean showWindow, Boolean waitForExit)
        {
            WriteLine("RunCmd " + cmd);

            Process p = new Process();
            ProcessStartInfo si = new ProcessStartInfo();
            String path = Environment.SystemDirectory;
            path = Path.Combine(path, @"cmd.exe");
            si.FileName = path;
            if (!cmd.StartsWith(@"/")) cmd = @"/c " + cmd;
            si.Arguments = cmd;
            si.UseShellExecute = false;
            si.CreateNoWindow = !showWindow;
            si.RedirectStandardOutput = true;
            si.RedirectStandardError = true;
            p.StartInfo = si;

            p.Start();
            if (waitForExit)
            {
                p.WaitForExit();

                String str = p.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(str))
                    WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
                str = p.StandardError.ReadToEnd();
                if (!String.IsNullOrEmpty(str))
                    WriteLine(str.Trim(new Char[] { '\r', '\n', '\t' }).Trim());
            }
        }

        /// <summary>执行SC命令</summary>
        /// <param name="cmd"></param>
        protected static void RunSC(String cmd)
        {
            String path = Environment.SystemDirectory;
            path = Path.Combine(path, @"sc.exe");
            if (!File.Exists(path)) path = "sc.exe";
            if (!File.Exists(path)) return;
            RunCmd(path + " " + cmd, false, true);
        }
        #endregion

        #region 获取依赖基础服务的服务集合
        /// <summary>
        /// 获取依赖于<paramref name="serviceName"/>实例的服务
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static ServiceController[] GetDependentServices(String serviceName = "XAgent")
        {
            var services = ServiceController.GetServices();
            if (serviceName.IsNullOrWhiteSpace()) serviceName = "XAgent";
            var sc = services.First(s => s.ServiceName == serviceName);
            if (sc == null) return null;
            return sc.DependentServices;
        }
        /// <summary>
        /// 获取依赖于<paramref name="serviceName"/>实例的服务名称
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public static String[] GetDependentServiceNames(String serviceName = "XAgent")
        {
            var scs = GetDependentServices(serviceName);
            if (scs == null) return null;
            return scs.Select(s => s.ServiceName).ToArray();
        }
        #endregion

        /// <summary>写日志</summary>
        /// <param name="msg"></param>
        public static void WriteLine(String msg)
        {
            if (XTrace.Debug) XTrace.WriteLine(msg);
        }
    }
}
