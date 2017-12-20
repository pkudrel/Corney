using System.IO;
using Autofac;
using Corney.Core;
using Corney.Core.Common.App;
using Corney.Core.Common.Io;
using Corney.Core.Features.App.Modules;

namespace Corney
{
    public class AppModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            CreateConfigAndRegistry(builder);
        }

        private void CreateConfigAndRegistry(ContainerBuilder builder)
        {
            var extendedRegistry = Bootstrap.Instance.GetExtendedRegistry();
            var configDirPath = Path.Combine(extendedRegistry.ConfigDir, Consts.APP_DIR);
            var configPath = Path.Combine(configDirPath, Consts.APP_CONFIG);

            Misc.CreateDirIfNotExist(configDirPath);
            var config =
                Misc.ReadJsonSafe<CorneyConfig>(configPath) ?? new CorneyConfig();
            var registry = new CorneyRegistry(extendedRegistry, configPath, config);
            builder.RegisterInstance(config).SingleInstance();
            builder.RegisterInstance(registry).SingleInstance();
        }
    }
}