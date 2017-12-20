using Autofac;
using Corney.Core.Features.Monitors.Services;

namespace Corney.Core.Features.Monitors.Config
{
    public class AutofacModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ConfigFileMonitorService>().AsSelf().SingleInstance();
        }
    }
}