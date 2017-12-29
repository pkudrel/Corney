using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Cron.Models;
using Corney.Core.Features.Processes.Services;
using NLog;

namespace Corney.Core.Features.Cron.Service
{
    public class CronService : ICronService
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();

        private readonly CorneyRegistry _corneyRegistry;

        private readonly Dictionary<string, List<CronDefinition>> _cronDefinitions =
            new Dictionary<string, List<CronDefinition>>(StringComparer.OrdinalIgnoreCase);

        private List<CronDefinition> _itemsToRunOnNextMinute;
        private IDisposable _nextSchedule;

        public CronService(CorneyRegistry corneyRegistry)
        {
            _corneyRegistry = corneyRegistry;
        }

        public void Start()
        {
            _log.Info("Start CronService");

            InitWork();
        }

        public void Restart()
        {
            _log.Info("Restart CronService");
            _nextSchedule?.Dispose();
            InitWork();
        }

        public void Stop()
        {
            _log.Info("Stop CronService");
            _nextSchedule?.Dispose();
        }

        private void InitWork()
        {
            CreateListDefinitions();
            FindNextItemToRun();
            var start = DateTime.UtcNow;
            var startDown = start.RoundDown(TimeSpan.FromSeconds(60));
            var next = startDown.AddMinutes(1);
            _log.Info($"Cron will be processing items from: {next}");
            GenerateNext(next);
            ScheduleNext(next);
        }

        private void GenerateNext(DateTime next)
        {
            _log.Debug($"GenerateNext; Set next timer: {next}; local: {next.ToLocalTime()}");

            // Dates in cron file are in "local time". We should conver next to local time. 
            var nextLocal = next.ToLocalTime();
            _itemsToRunOnNextMinute = _cronDefinitions
                .SelectMany(x => x.Value).Where(x => x.Expression.GetNextOccurrence(DateTime.UtcNow) == nextLocal)
                .ToList();

            _log.Debug($"GenerateNext; Items to run on next minute count: { _itemsToRunOnNextMinute.Count}");
        }

        private void Execute(DateTime date)
        {
            _log.Info($"Execute; {date} ");

            if (_itemsToRunOnNextMinute.Any())
            {
                foreach (var cronDefinition in _itemsToRunOnNextMinute)
                {
                    var processWrapper = new ProcessWrapper();
                    var t1 = Pharse.Tokenize(cronDefinition.ExecutePart);
                    processWrapper.Start(t1, "");
                }
                _itemsToRunOnNextMinute.Clear();
                FindNextItemToRun();
            }


            var next = date.AddMinutes(1);
            ScheduleNext(next);
        }

        private void FindNextItemToRun()
        {
            var nextItemToRun = _cronDefinitions
                .SelectMany(x => x.Value)
                .OrderBy(x => x.Expression.GetNextOccurrence(DateTime.UtcNow))
                .FirstOrDefault();

            if (nextItemToRun != null)
            {
                var item = nextItemToRun.Expression.GetNextOccurrence(DateTime.UtcNow);
                if (item.HasValue)
                {
                    _log.Info(
                        $"Next item to run at: {item}; " +
                        $"Next item to run at (local): {item.Value.ToLocalTime()}; " +
                        $"Execute: {nextItemToRun.ExecutePart}");
                }
                else
                {
                    _log.Info("Canot find next item to run");
                }

            }

        }

        private void ScheduleNext(DateTime next)
        {
            _log.Debug($"ScheduleNext; Set next timer: {next}");
            var d = new DateTimeOffset(next);
            _nextSchedule = Observable
                .Timer(d, Scheduler.CurrentThread)
                .Timestamp()
                .Subscribe(
                    x =>
                    {
                        _log.Debug($"ScheduleNext; Befor execute: {x}");
                        Task.Run(() => Execute(next));
                        //Execute(next);
                        _log.Debug($"ScheduleNext; After execute: {x}");
                    });
        }

        private void CreateListDefinitions()
        {
            _cronDefinitions.Clear();
            foreach (var crontabFile in _corneyRegistry.CrontabFiles)
            {
                var list = CrontabFileParser.Read(crontabFile);
                _cronDefinitions.Add(crontabFile, list);
            }
        }
    }

    public interface ICronService
    {
        void Start();
        void Restart();
        void Stop();
    }
}