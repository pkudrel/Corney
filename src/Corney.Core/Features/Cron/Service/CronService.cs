using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Corney.Core.Features.App.Modules;
using Corney.Core.Features.Cron.Models;

namespace Corney.Core.Features.Cron.Service
{
    public class CronService : ICronService
    {
        private readonly CorneyRegistry _corneyRegistry;

        private readonly Dictionary<string, List<CronDefinition>> _cronDefinitions =
            new Dictionary<string, List<CronDefinition>>(StringComparer.OrdinalIgnoreCase);

        public CronService(CorneyRegistry corneyRegistry)
        {
            _corneyRegistry = corneyRegistry;
        }

        public CronDefinition[] S => _cronDefinitions
            .SelectMany(x => x.Value)
            .OrderBy(x => x.Expression.GetNextOccurrence(DateTime.UtcNow))
            .TakeWhile(x => x.Next < DateTime.Now.AddDays(1))
            .ToArray();

        public void Start()
        {
            CreateListDefinitions();

            var d = _cronDefinitions
                .SelectMany(x => x.Value)
                .OrderBy(x => x.Expression.GetNextOccurrence(DateTime.UtcNow))
                .TakeWhile(x => x.Next < DateTime.Now.AddDays(1))
                .ToArray();
        }


        private void Select()
        {
            var t = S.FirstOrDefault();
            var rr = t.Expression.GetNextOccurrence(DateTime.UtcNow).Value;
            Observable
                .Timer(rr).Timestamp()
                .Subscribe(
                    x =>
                    {
                        // Do Stuff Here
                        Console.WriteLine(x.Timestamp);
                        // Console WriteLine Prints
                        // 0
                    });
        }

        private void CreateListDefinitions()
        {
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
    }
}