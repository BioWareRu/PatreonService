using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace PatreonService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateApplication(args).RunAsync<Startup>();
        }

        // need for migrations
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            CreateApplication(args).CreateBasicHostBuilder<Startup>();

        public static PatreonApplication CreateApplication(string[] args) =>
            new(args);
    }
}