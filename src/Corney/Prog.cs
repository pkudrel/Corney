using System;
using System.Reactive.Linq;

namespace Corney
{
    public class Prog
    {
        public static void Main()
        {
            var dt = DateTime.Now;
            var dtNext = dt.AddSeconds(3);

            Console.WriteLine("{0} - {1}", dt, dtNext);

            var r = Observable
                .Timer(dtNext);

            Observable
                .Timer(dtNext).Timestamp()
                .Subscribe(
                    x =>
                    {
                        // Do Stuff Here
                        Console.WriteLine(x.Timestamp);
                        // Console WriteLine Prints
                        // 0
                    });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }


        /*
         *
         *
        
         var dt = DateTime.Now;
            var dtNext = dt.AddSeconds(10);

            var observable = Observable.Timer(dtNext).Timestamp();
            using (observable.Subscribe(
                x => Console.WriteLine("{0}: {1}", x.Value, x.Timestamp)))
            {
                Console.WriteLine("Press any key to unsubscribe");
                Console.ReadKey();
            }

         *
         *
         */
    }
}