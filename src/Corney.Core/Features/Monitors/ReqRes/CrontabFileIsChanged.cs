using System.Collections.Generic;
using MediatR;

namespace Corney.Core.Features.Monitors.ReqRes
{
    public class CrontabFileIsChanged : INotification
    {
        public string[] CronFiles { get; }

        public CrontabFileIsChanged(string[] cronFiles)
        {
            CronFiles = cronFiles;
        }
    }
}