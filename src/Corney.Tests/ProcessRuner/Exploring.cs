using Corney.Core.Features.Processes.Services;
using Xunit;

namespace Corney.Tests.ProcessRuner
{
    public class Exploring
    {
        [Fact]
        public void FactMethodName()
        {
            var ss = @"eo.exe -clients=client1,client2 -waitTime=10 -SeriesMode=2";
            var t1 = Pharse.Tokenize(ss);
            ProcessWrapper processWrapper = new ProcessWrapper();
            processWrapper.Start(t1, "");
            //var t = new ProcessWrapper("'SiteScrapera.exe\a\a\' -clients=client1,client2 -waitTime=10 -SeriesMode=2", "", false, false);
        }
    }
}