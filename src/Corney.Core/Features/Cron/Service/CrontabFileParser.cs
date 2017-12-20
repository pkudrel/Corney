using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Corney.Core.Features.Cron.Models;
using Cronos;

namespace Corney.Core.Features.Cron.Service
{
    public class CrontabFileParser
    {
        private static readonly char[] _delimiterChars = {' ', '\t'};

        public static List<CronDefinition> Read(string crontabFile)
        {
            var l = new List<CronDefinition>();
            var s = File.ReadAllLines(crontabFile);
            var source = s.Where(x => x != null && !string.IsNullOrEmpty(x) & !x.Trim().StartsWith("#")).ToArray();

            foreach (var s1 in source)
            {
                var arr = s1.Split(_delimiterChars, 6, StringSplitOptions.RemoveEmptyEntries);
                var executePart = arr[5];
                var cronPart = string.Join(" ", arr.Take(5));
                var expression = CronExpression.Parse(cronPart, CronFormat.Standard);
                var next = expression.GetNextOccurrence(DateTime.UtcNow);
                var r = new CronDefinition(cronPart, executePart, expression, next);
                l.Add(r);
            }
            return l;
        }
    }
}