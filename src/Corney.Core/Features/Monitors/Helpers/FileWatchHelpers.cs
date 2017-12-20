using System;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Corney.Core.Features.Monitors.Helpers
{
    public class FileWatchHelpers
    {
       public static IObservable<FileSystemEventArgs> CreateForFile(string path)
        {
            var dir = Path.GetDirectoryName(path);
            var filter = Path.GetFileName(path);

            return Observable.Create<FileSystemEventArgs>(subj =>
            {
                var disp = new CompositeDisposable();

                var fsw = new FileSystemWatcher(
                    dir ?? throw new InvalidOperationException(),
                    filter ?? throw new InvalidOperationException())
                {
                    NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                                            | NotifyFilters.FileName
                };


                disp.Add(fsw);

                var allEvents = Observable.Merge(
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Changed += x,
                        x => fsw.Changed -= x),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Created += x,
                        x => fsw.Created -= x),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(x => fsw.Deleted += x,
                        x => fsw.Deleted -= x));

                disp.Add(allEvents.Throttle(TimeSpan.FromMilliseconds(250))
                    .Select(x => x.EventArgs)
                    .Synchronize(subj)
                    .Subscribe(subj));

                fsw.EnableRaisingEvents = true;
                return disp;
            }).Publish().RefCount();
        }
    }
}