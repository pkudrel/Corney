using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        private readonly List<IDisposable> _cfd =
            new List<IDisposable>();

        private readonly CompositeDisposable _configDisposable = new CompositeDisposable();
        private readonly string _configFilePath;


        private readonly IMediator _mediator;

        private IObservable<FileSystemEventArgs> _configFileObservable;
        private int counter;


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
            ClearDisposable();
        }


        private void ClearDisposable()
        {
            foreach (var disposable in _cfd) disposable.Dispose();
            _cfd.Clear();
        }

        public void Initialize()
        {
            _configFileObservable = FileWatchHelpers.CreateForFile(_configFilePath);
            _configDisposable.Add(_configFileObservable.Subscribe(OnConfigFileChanged));
            var files = MonitorFilesInConfig(_configFilePath);
        }


        private void OnConfigFileChanged(FileSystemEventArgs x)
        {
            _log.Debug(
                $"OnConfigFileChanged; Counter: {counter++}; Type: {x.ChangeType}; FullPath: {x.FullPath}; Name: {x.Name}");
            switch (x.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    break;
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed:

                    var files = MonitorFilesInConfig(_configFilePath);
                    _mediator.Publish(new CrontabFileIsChanged(files));
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void OneCrontabFileMonitor(FileSystemEventArgs x)
        {
            _log.Debug(
                $"OneCrontabFileMonitor; Counter: {counter++}; Type: {x.ChangeType}; FullPath: {x.FullPath}; Name: {x.Name}");
            switch (x.ChangeType)
            {
                case WatcherChangeTypes.Created:

                    break;
                case WatcherChangeTypes.Deleted:
                    break;
                case WatcherChangeTypes.Changed:
                    var files = MonitorFilesInConfig(_configFilePath);
                    _mediator.Publish(new CrontabFileIsChanged(files));
                    break;
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private string[] MonitorFilesInConfig(string file)
        {
            var list = new HashSet<string>();

            ClearDisposable();


            var config = Misc.ReadJson<CorneyConfig>(file);
            var filesToObserve = config.CrontabFiles.Distinct().ToArray();
            _log.Debug($"Files to monitor: {filesToObserve.Count()}");
            var cronfilesObservable = new List<IObservable<FileSystemEventArgs>>();
            foreach (var filesCrontabFile in filesToObserve)
            {
                _log.Debug($"File to monitor: {filesCrontabFile}");
                var cronFile = FileWatchHelpers.CreateForFile(filesCrontabFile);
                cronfilesObservable.Add(cronFile);
                list.Add(filesCrontabFile);
            }

           
            var dis = cronfilesObservable.Merge().Subscribe(OneCrontabFileMonitor);
            _cfd.Add(dis);
            return list.ToArray();
        }
    }
}