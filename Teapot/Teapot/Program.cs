using System;
using System.Threading;
using System.Diagnostics;
using nanoFramework.Networking;
using nanoFramework.WebServer;
using Microsoft.Extensions.DependencyInjection;
using nanoFramework.Hosting;
using Teapot.Services;
using System.Device.Wifi;

namespace Teapot
{
    public class Program
    {
        private static string MySsid = "your_ssid";
        private static string MyPassword = "your_password";
        private static bool _isConnected = false;
        public static void Main()
        {
            try
            {
                WifiAdapter wifi = WifiAdapter.FindAllAdapters()[0];
                Debug.WriteLine("Waiting for network up and IP address...");
                bool success;
                CancellationTokenSource cs = new(60000);
                success = WifiNetworkHelper.ConnectDhcp(MySsid, MyPassword, requiresDateTime: false, token: cs.Token);
                if (!success)
                {
                    Debug.WriteLine($"Can't get a proper IP address and DateTime, error: {WifiNetworkHelper.Status}.");
                    if (WifiNetworkHelper.HelperException != null)
                    {
                        Debug.WriteLine($"Exception: {WifiNetworkHelper.HelperException}");
                    }
                    return;
                }

                Console.WriteLine($"Connected to network {MySsid}");

                var serviceProvider = ConfigureServices();

                // Instantiate a new web server on port 80.
                using (var webServer = new WebServerDi(80, HttpProtocol.Http, new Type[] { typeof(TeapotController) }, serviceProvider))
                {
                    webServer.Start();
                    IHost host = CreateHostBuilder().Build();
                    host.Run();
                    Thread.Sleep(Timeout.Infinite);
                }

            }
            catch (Exception ex)
            {

                Debug.WriteLine($"{ex}");
            }
                        
        }
        private static ServiceProvider ConfigureServices()
        {
            return new ServiceCollection()
                .AddSingleton(typeof(IStateService), typeof(StateService))
                .AddSingleton(typeof(IHardwareService), typeof(HardwareService))
                .AddHostedService(typeof(TeapotService))
                .BuildServiceProvider();
        }
        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
              .ConfigureServices(services =>
                {
                    services.AddSingleton(typeof(IStateService), typeof(StateService));
                    services.AddSingleton(typeof(IHardwareService), typeof(HardwareService));
                    services.AddHostedService(typeof(TeapotService));

                });
    }
}
