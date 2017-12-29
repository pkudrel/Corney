using System;
using System.Windows.Forms;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Cron.ReqRes;
using Corney.Properties;
using MediatR;

namespace Corney
{
    public class CorneyContext : ApplicationContext
    {
        private readonly CorneyRegistry _registry;
        private readonly IMediator _mediator;
        private readonly NotifyIcon _trayIcon;

        public CorneyContext(CorneyRegistry registry, IMediator mediator)
        {
            _registry = registry;
            _mediator = mediator;
            // Initialize Tray Icon
            _trayIcon = new NotifyIcon
            {
                Icon = Resources.AppIco,
                ContextMenu = new ContextMenu(new[]
                {
                    new MenuItem($"Corney - {_registry.AppVersion.SemVer}"), 
                    new MenuItem("Exit", Exit)
                }),
                Visible = true
            };
        }

        private async void Exit(object sender, EventArgs e)
        {

          await  _mediator.Publish(new StopCorneyReq());

            // Hide tray icon, otherwise it will remain shown until user mouses over it
            _trayIcon.Visible = false;

            Application.Exit();
        }
    }
}