using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Corney.Core;
using Corney.Core.Common.App;
using Corney.Core.Common.App.ReqRes;
using Corney.Core.Common.Infrastructure;
using Corney.Core.Features.Cron.ReqRes;
using MediatR;
using Microsoft.Win32.SafeHandles;
using NLog;
using Topshelf;
using Topshelf.Autofac;

namespace Corney
{
    internal static class Program
    {
        private const int _SW_HIDE = 0;
        const int _SW_SHOW = 5;

        private const int _STD_OUTPUT_HANDLE = -11;
        private const int _MY_CODE_PAGE = 437;
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private static readonly bool createConsole = true;
        private static bool _hideConsole = true;
        private static bool _executeTimer = false; 

        [DllImport("kernel32.dll", EntryPoint = "GetStdHandle", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true, CharSet = CharSet.Auto,
            CallingConvention = CallingConvention.StdCall)]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        public static void Main(string[] args)
        {
            _hideConsole = args.Any(x => x.ToLower() == "-hide");
            _executeTimer = _hideConsole;
            if (createConsole)
            {
                AllocConsole();

                if (_hideConsole)
                {
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, _SW_HIDE);
                }

                var stdHandle = GetStdHandle(_STD_OUTPUT_HANDLE);
                var safeFileHandle = new SafeFileHandle(stdHandle, true);
                var fileStream = new FileStream(safeFileHandle, FileAccess.Write);
                var encoding = Encoding.GetEncoding(_MY_CODE_PAGE);
                var standardOutput = new StreamWriter(fileStream, encoding);
                standardOutput.AutoFlush = true;
                Console.SetOut(standardOutput);
            }

            var res = MainLowLevel();
            if (res.Error) return;
            MainAsync(res.Instance.Value).GetAwaiter().GetResult();
        }

        private static (Attempt<SingleGlobalInstance> Instance, bool Error) MainLowLevel()
        {
            try
            {
                // Only one program instance
                var attempt = Attempt.Get(() => new SingleGlobalInstance());
                if (attempt.Failed)
                {
                    SingleInstanceHelper.ProcessFailedSingleInstance(attempt);
                    return (null, true);
                }

                var currentAssembly = typeof(Program).Assembly;

                // Init app internal registry 
                Bootstrap.Instance.GetExtendedRegistry(currentAssembly);

                // Add all assemblies in our scope of interest
                AssemblyCollector.Instance.AddAssembly(currentAssembly, AssemblyInProject.View);
                AssemblyCollector.Instance.AddAssembly(typeof(Main).Assembly, AssemblyInProject.Core);


                return (attempt, false);
            }
            catch (Exception e)
            {
                _log.Error(e);
                _log.Error(e.Message);
            }

            return (null, true);
        }

        public static async Task MainAsync(SingleGlobalInstance instance)
        {
            using (instance)
            {
                var builder = new ContainerBuilder();
                builder.RegisterAssemblyModules(AssemblyCollector.Instance.GetAssemblies());
                try
                {
                    using (var container = builder.Build())
                    {
                        using (var scope = container.BeginLifetimeScope())
                        {
                            var mediator = scope.Resolve<IMediator>();

                            // Any configuration checks, initializations should be handled by this event
                            await mediator.Publish(new AppStartingEvent());
                            await mediator.Publish(new AppStartedEvent());


                            HostFactory.Run(x => //1
                            {
                                x.UseAutofacContainer(scope);
                                x.Service<TownCrier>(s =>
                                {
                                    //s.ConstructUsingAutofacContainer();
                                    s.ConstructUsing(name => new TownCrier(_executeTimer));
                                    s.WhenStarted(async tc =>
                                    {
                                        await mediator.Publish(new StartCorneyReq());
                                        tc.Start();
                                    });
                                    s.WhenShutdown(tc => { tc.Stop(); });
                                    s.WhenStopped(tc =>
                                    {
                                        tc.Stop();
                                        //scope?.Dispose();
                                        //container?.Dispose();
                                    });
                                });
                                x.RunAsLocalSystem();

                                x.SetDescription("Corney - Crontab file executor for Windows");
                                x.SetDisplayName("Corney");
                                x.SetServiceName("Corney");
                            });
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e);
                    _log.Error(e.Message);
                    var handle = GetConsoleWindow();
                    ShowWindow(handle, _SW_SHOW);
                }
            }
        }
    }
}