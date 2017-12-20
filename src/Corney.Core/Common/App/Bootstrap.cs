using System;
using System.IO;
using System.Reflection;
using Common.Io;
using Common.Version;
using Corney.Core.Common.App.Models;
using Newtonsoft.Json;
using NLog;
using NLog.Config;

namespace Corney.Core.Common.App
{
    public class Bootstrap
    {
        private const string _WORK_DIR = "work-dir";
        private const string _GLOBAL_DIR = "global-dir";
        private const string _LOG_DIR = "log";
        private const string _DEV_DIR = ".dev";
        private const string _GIT_DIR = ".git";
        private const string _APP_VS_DIR = ".app.vs";
        private const string _SYRUP_DIR = ".syrup";
        private const string _DEV_FILE = "dev.json";
        private const string _CONFIG_DIR = "config";
        private static readonly Bootstrap _instance = new Bootstrap();

        private static ExtendedRegistry _extendedRegistryValue;
        private static readonly object _padlock = new object();

        static Bootstrap()
        {
        }

        private Bootstrap()
        {
        }

        // ReSharper disable once ConvertToAutoProperty
        public static Bootstrap Instance => _instance;

        public ExtendedRegistry GetExtendedRegistry(Assembly asm)
        {
            if (_extendedRegistryValue == null)
                lock (_padlock)
                {
                    if (_extendedRegistryValue != null) return _extendedRegistryValue;
                    _extendedRegistryValue = GetTemporaryRegistryImpl(asm);
                }

            return _extendedRegistryValue;
        }


        public ExtendedRegistry GetExtendedRegistry()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

            return GetExtendedRegistry(asm);
        }

        private ExtendedRegistry GetTemporaryRegistryImpl(Assembly asm)
        {

            var res = new ExtendedRegistry();
            res.AssemblyName = asm.GetName().Name;
            res.AppVersion = VersionGenerator.Init(asm);
            res.MachineName = Environment.MachineName;
            res.AssemblyFilePath = new Uri(asm.CodeBase).LocalPath;
            res.Is64BitProcess = Environment.Is64BitProcess;
            res.ExeFileDir = Path.GetDirectoryName(res.AssemblyFilePath);
            res.SyrupDir = FindDir(res.ExeFileDir, _SYRUP_DIR);
            res.IsSyrup = !string.IsNullOrEmpty(res.SyrupDir);

            res.GitDir = FindDir(res.ExeFileDir, _GIT_DIR);
            res.DevDir = FindDevDir(res.ExeFileDir, _DEV_DIR, res.SyrupDir);
            res.RootDir = GetRoot(res.ExeFileDir, res.SyrupDir, res.DevDir);
            res.RepositoryRootDir = GetRepositoryRoot(res.GitDir);
            res.IsDeveloperMode = IsDeveloperMode(res.DevDir);
            var dv = GetDevSettings(res.DevDir);


            res.WorkDir = GetWorkDir(res.RootDir);
            res.LogDir = GetSimpleDir(res.RootDir, _LOG_DIR, res.IsDeveloperMode);
            res.ConfigDir = GetSimpleDir(res.RootDir, _CONFIG_DIR, res.IsDeveloperMode);
            res.GlobalDir = Path.Combine(res.RootDir, _GLOBAL_DIR);


           
            res.IsDeveloperMode = dv.IsDeveloperMode && !dv.DeveloperConfig.IgnoreMe;

            if (res.IsDeveloperMode) ExecuteDevModel(res, dv);
#if DEBUG
            res.IsDebug = true;
#else
            res.IsDebug = false;
#endif
            Misc.CreateDirIfNotExist(res.ConfigDir);
            Misc.CreateDirIfNotExist(res.LogDir);
            return res;
        }


        private string GetSimpleDir(string rootDir, string dirName, bool useShort)
        {
            return useShort ? Path.Combine(rootDir, dirName) : Path.Combine(rootDir, dirName, Environment.MachineName);
        }

        private string GetWorkDir(string rootDir)
        {
            return Path.Combine(rootDir, _WORK_DIR);
        }


        private bool IsDeveloperMode(string devDir)
        {
            if (!string.IsNullOrEmpty(devDir))
            {
                var devConfig = Path.Combine(devDir, _DEV_FILE);

                if (File.Exists(devConfig))
                    return true;
            }

            return false;
        }

        private string GetRepositoryRoot(string gitDir)
        {
            var parentGit = string.IsNullOrEmpty(gitDir)
                ? string.Empty
                : new DirectoryInfo(gitDir)?.Parent?.FullName;

            return parentGit;
        }

        private void ExecuteDevModel(ExtendedRegistry res, DevSettings dv)
        {
            if (!string.IsNullOrEmpty(dv.DeveloperConfig.NLogConfigFile))
            {
                var pathToNLog = Path.Combine(res.ConfigDir, dv.DeveloperConfig.NLogConfigFile);
                LogManager.Configuration = new XmlLoggingConfiguration(pathToNLog, true);
                LogManager.Configuration.Variables["logDirectory"] = res.LogDir;
                LogManager.Configuration.Variables["appName"] = res.AssemblyName.Replace('.', '-').GenerateSlug();
                // LogManager.Configuration.Reload();
            }
        }


        private string GetRoot(string appDir, string syrupDir, string devDir)
        {
            var parentSyrup = string.IsNullOrEmpty(syrupDir)
                ? string.Empty
                : new DirectoryInfo(syrupDir)?.Parent?.FullName;


            if (!string.IsNullOrEmpty(syrupDir)) return parentSyrup;
            var configDirInApp = Path.Combine(appDir, _CONFIG_DIR);
            if (Directory.Exists(configDirInApp)) return appDir;

            if (!string.IsNullOrEmpty(devDir))
            {
                var vsAppDir = Path.Combine(devDir, _APP_VS_DIR);
                if (Directory.Exists(vsAppDir)) return vsAppDir;
            }

            return appDir;
        }


        private DevSettings GetDevSettings(string devDir)
        {
            var res = new DevSettings();
            if (!Directory.Exists(devDir)) return res;
            var devConfig = Path.Combine(devDir, _DEV_FILE);
            if (!File.Exists(devConfig)) return res;
            var json = File.ReadAllText(devConfig);
            var conf = JsonConvert.DeserializeObject<DeveloperConfig>(json);
            res.IsDeveloperMode = true;
            res.IsDeveloperMode = true;
            res.DeveloperConfig = conf;
            return res;
        }


        private string FindDir(string startPath, string dirToFind)
        {
            var di = new DirectoryInfo(startPath);
            while (true)
            {
                var path = Path.Combine(di.FullName, dirToFind);
                if (Directory.Exists(path))
                    return path;

                if (di.Parent == null) return null;
                di = di.Parent;
            }
        }

        private string FindDevDir(string startPath, string dirToFind, string syrupDir)
        {
            if (!string.IsNullOrEmpty(syrupDir))
                if (Directory.Exists(syrupDir))
                {
                    var di = new DirectoryInfo(syrupDir);
                    if (di.Parent != null)
                    {
                        var p = Path.Combine(di.Parent.FullName, dirToFind);
                        return File.Exists(p) ? p : null;
                    }
                }

            return FindDir(startPath, dirToFind);
        }

        internal class DevSettings
        {
            public bool IsDeveloperMode { get; set; }
            public DeveloperConfig DeveloperConfig { get; set; }
        }
    }
}