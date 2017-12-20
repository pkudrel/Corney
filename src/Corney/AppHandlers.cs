using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Corney.Core.Common.App.ReqRes;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Monitors.Services;
using MediatR;
using NLog;

namespace Corney
{
    public class AppHandlers : INotificationHandler<AppStartingEvent>
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly CorneyConfig _config;
        private readonly CorneyRegistry _registry;
        private readonly ConfigFileMonitorService _configFileMonitorService;

        // SiteScraperRegistry


        public AppHandlers(CorneyConfig config, CorneyRegistry registry, ConfigFileMonitorService configFileMonitorService)
        {
            _config = config;
            _registry = registry;
            _configFileMonitorService = configFileMonitorService;
        }

       

        public Task Handle(AppStartingEvent notification, CancellationToken cancellationToken)
        {
            _configFileMonitorService.Initialize();
            ;
            return Task.CompletedTask;
        }
    }
}