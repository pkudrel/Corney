﻿using Corney.Core.Common.App;

namespace Corney.Core.Features.App.Modules
{
    public class CorneyRegistry
    {
     
        public CorneyRegistry(ExtendedRegistry extendedRegistry, string configPath, CorneyConfig config)
        {
            ConfigFilePath = configPath;
            CrontabFiles = config.CrontabFiles;
        }

        public string ConfigFilePath { get; }
        public string[] CrontabFiles { get; }
    }
}