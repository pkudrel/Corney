using MediatR;

namespace Corney.Core.Features.Monitors.ReqRes
{
    public class CrontabFileIsChanged : INotification
    {
        public string File { get; }

        public CrontabFileIsChanged(string file)
        {
            File = file;
        }
    }
}