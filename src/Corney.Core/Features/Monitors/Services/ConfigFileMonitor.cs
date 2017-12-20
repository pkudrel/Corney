using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Corney.Core.Common.Io;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Monitors.Helpers;
using Corney.Core.Features.Monitors.ReqRes;
using MediatR;
using NLog;

namespace Corney.Core.Features.Monitors.Services
{
    public class ConfigFileMonitorService : IDisposable
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private readonly CompositeDisposable _configDisposable = new CompositeDisposable();

        private readonly string _configFilePath;
        private readonly CompositeDisposable _cronFilesDisposable = new CompositeDisposable();

        private readonly List<IObservable<FileSystemEventArgs>> _cronfilesObservable =
            new List<IObservable<FileSystemEventArgs>>();

        private readonly IMediator _mediator;

        private IObservable<FileSystemEventArgs> _configFileObservable;


        public ConfigFileMonitorService(IMediator mediator, CorneyRegistry registry)
        {
            _mediator = mediator;
            _configFilePath = registry.ConfigFilePath;
            Registry = registry;
        }

        public CorneyRegistry Registry { get; }

        public void Dispose()
        {
            _log.Debug("Dispose");
            _configDisposable.Dispose();
            _cronFilesDisposable.Dispose();
        }


        public void Initialize()
        {
            _configFileObservable = FileWatchHelpers.CreateForFile(_configFilePath);
            _configDisposable.Add(_configFileObservable.Subscribe(ConfigMonitor));
            OnConfigChange(_configFilePath);
        }

        private void ConfigMonitor(FileSystemEventArgs x)
        {
            switch (x.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    break;
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed:
                    OnConfigChange(x.FullPath);
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _log.Debug($"Type: {x.ChangeType}; FullPath: {x.FullPath}; Name: {x.Name}");
        }

        private void CrontabFileMonitor(FileSystemEventArgs x)
        {
            switch (x.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    OnCrontabFileChange(x.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed:
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _log.Debug($"Type: {x.ChangeType}; FullPath: {x.FullPath}; Name: {x.Name}");
        }

        private void OnCrontabFileChange(string xFullPath)
        {
            _mediator.Publish(new CrontabFileIsChanged(xFullPath));
        }

        private void OnConfigChange(string file)
        {
            _cronFilesDisposable.Clear();
            _cronfilesObservable.Clear();
            var config = Misc.ReadJson<CorneyConfig>(file);
            foreach (var filesCrontabFile in config.CrontabFiles)
            {
                var cronFile = FileWatchHelpers.CreateForFile(filesCrontabFile);
                _cronfilesObservable.Add(cronFile);
            }

            _configDisposable.Add(_cronfilesObservable.Merge().Subscribe(CrontabFileMonitor));
        }
    }
}