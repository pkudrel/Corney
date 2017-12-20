using Autofac;
using Corney.Core.Features.Cron.Service;

namespace Corney.Core.Features.Cron.Configs
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CronService>().As<ICronService>().SingleInstance();
        }
    }
}