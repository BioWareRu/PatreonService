using System.Threading.Tasks;
using BioEngine.BRC.Common;

namespace PatreonService
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var bioEngine = new BioEngine.Core.BioEngine(args)
                .AddLogging()
                .AddS3Client();

            await bioEngine.RunAsync<Startup>();
        }
    }
}
