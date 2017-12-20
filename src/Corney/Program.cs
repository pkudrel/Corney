using System;
using System.Threading.Tasks;
using Autofac;
using Corney.Core;
using Corney.Core.Common.App;
using Corney.Core.Common.App.ReqRes;
using Corney.Core.Common.Infrastructure;
using Corney.Core.Features.Cron.ReqRes;
using MediatR;
using NLog;
using Topshelf;
using Topshelf.Autofac;

namespace Corney
{
    internal static class Program
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        public static void Main2()
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
                            await mediator.Send(new StartCorneyReq());


                            HostFactory.Run(x => //1
                            {
                                x.UseAutofacContainer(scope);
                                x.Service<TownCrier>(s =>
                                {
                                    //s.ConstructUsingAutofacContainer();
                                    s.ConstructUsing(name => new TownCrier());
                                    s.WhenStarted(async tc =>
                                    {
                                        await mediator.Send(new StartCorneyReq());
                                        tc.Start();
                                    });
                                    s.WhenShutdown(tc =>
                                    {

                                        tc.Stop();

                                    });
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
                }
            }
        }
    }
}