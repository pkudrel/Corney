using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac;
using Common.Version;
using Corney.Core;
using Corney.Core.Common.App;
using Corney.Core.Common.App.ReqRes;
using Corney.Core.Common.Infrastructure;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Cron.ReqRes;
using MediatR;
using NLog;

namespace Corney
{
    /// <summary>
    /// Two important functions:
    /// 1. MainLowLevel: Check if is only one instance, create extended registry, add assemblies, etc. Any errors should be
    /// handled in this file. No domain configuration in any form. After this function program must have ExtendedRegistry
    /// object and well initialized file log
    /// 2. MainAsync: Registration all AutoFac modules, domain configuration, start main user code
    /// Scenario:
    /// a) Registration of all AutoFac modules (module code per features should be in feature/config folder (by
    /// convention).
    /// Exception: AppModule.cs - in root. In this file you should crate Registry and other important domain objects
    /// b) Publishing 'AppStartingEvent' with Mediator. Domain initialization and checks. Handlers should be in
    /// feature/handlers folder (by convention)
    /// Exception: AppHandlers.cs - in root. Important checks and  initializations
    /// </summary>
    internal static class App
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main()
        {
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

                var currentAssembly = typeof(App).Assembly;
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
                MessageBox.Show(e.Message, "Critical ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            _log.Info($"Aplication: {VersionGenerator.GetVersion().FullName}");

                            var mediator = scope.Resolve<IMediator>();
                            var registry = scope.Resolve<CorneyRegistry>();

                            // Any configuration checks, initializations should be handled by this event
                            // Most important start code is in the AppHandlers class. 
                            // You may extend it as you wont. 
                            await mediator.Publish(new AppStartingEvent());

                            // And after 
                            await mediator.Publish(new AppStartedEvent());

                            await mediator.Publish(new StartCorneyReq(registry.CrontabFiles));


                            Application.EnableVisualStyles();
                            Application.SetCompatibleTextRenderingDefault(false);
                            Application.Run(new CorneyContext(registry, mediator));
                        }
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e);
                    _log.Error(e.Message);
                    MessageBox.Show(e.Message, "Critical ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}