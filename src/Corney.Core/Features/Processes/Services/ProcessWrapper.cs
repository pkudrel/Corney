using System;
using System.Diagnostics;
using System.IO;
using Corney.Core.Features.Cron.Models;
using NLog;

namespace Corney.Core.Features.Processes.Services
{
    public class ProcessWrapper
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();


        public void Start(ExecuteItem item, string directory)
        {
            var process = new Process();
            if (string.IsNullOrEmpty(directory))
            {
                directory = Path.GetFullPath(item.Program);
            }

            try
            {
                _log.Debug("Executing: " + item.Program);
                _log.Debug("Parameters: " + item.Arguments);
                _log.Debug("In directory: " + directory);

                process.StartInfo.FileName = item.Program;
                process.StartInfo.Arguments = item.Arguments;
                process.StartInfo.WorkingDirectory = directory;
                //process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                //process.StartInfo.CreateNoWindow = true;
                //process.StartInfo.RedirectStandardInput = true;
                //process.StartInfo.RedirectStandardOutput = RedirectAllOutput;
                //process.StartInfo.RedirectStandardError = RedirectErrors;
                //process.StartInfo.UseShellExecute = false;
                process.Start();
            }
            catch (Exception e)
            {
                _log.Error(e);
            }
        }
    }
}