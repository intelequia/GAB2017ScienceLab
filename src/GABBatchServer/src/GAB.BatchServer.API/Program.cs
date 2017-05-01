using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace GAB.BatchServer.API
{
    /// <summary>
    /// Program entry class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Program entry point
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                //.UseKestrel(options =>
                //{
                //    // options.ThreadCount = 4;
                //    options.NoDelay = true;
                //    options.UseHttps("testCert.pfx", "testPassword");
                //})
                //.UseUrls("http://localhost:5000", "https://localhost:5001")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
