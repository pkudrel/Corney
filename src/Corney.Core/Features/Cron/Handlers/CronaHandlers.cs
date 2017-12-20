﻿using System.Threading;
using System.Threading.Tasks;
using Corney.Core.Features.Cron.ReqRes;
using Corney.Core.Features.Cron.Service;
using Corney.Core.Features.Monitors.ReqRes;
using MediatR;

namespace Corney.Core.Features.Cron.Handlers
{
    public class CronHandlers : 
        INotificationHandler<CrontabFileIsChanged>,
        IRequestHandler<StartCorneyReq>
    {
        private readonly ICronService _cronService;

        public CronHandlers(ICronService cronService)
        {
            _cronService = cronService;
        }

        public Task Handle(CrontabFileIsChanged notification, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task Handle(StartCorneyReq message, CancellationToken cancellationToken)
        {
            _cronService.Start();
            throw new System.NotImplementedException();
        }
    }
}