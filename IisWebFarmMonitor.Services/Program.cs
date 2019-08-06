using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Autofac.Integration.ServiceFabric;

using Cogito.AspNetCore;
using Cogito.AspNetCore.Autofac;
using Cogito.Autofac;
using Cogito.Autofac.DependencyInjection;
using Cogito.ServiceFabric;
using Cogito.ServiceFabric.AspNetCore.Kestrel.Autofac;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IisWebFarmMonitor.Services
{

    public static class Program
    {

        /// <summary>
        /// Main application entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Task Main(string[] args)
        {
            return FabricEnvironment.IsFabric ? RunFabric(args) : RunConsole(args);
        }

        /// <summary>
        /// Runs the application in Console mode.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task RunConsole(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();

            using (var hostScope = builder.Build())
                await WebHost.CreateDefaultBuilder(args)
                    .UseStartup<WebService>(hostScope)
                    .UseKestrel()
                    .BuildAndRunAsync();
        }

        /// <summary>
        /// Main application entry point.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task RunFabric(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();
            builder.RegisterServiceFabricSupport();
            builder.RegisterStatelessService<MonitorService>("MonitorServiceType");
            builder.RegisterStatelessKestrelWebService<WebService>("WebServiceType");
            builder.RegisterActor<MonitorActor>();
            builder.Populate(s => s.AddLogging());

            using (builder.Build())
                await Task.Delay(Timeout.Infinite);
        }

    }

}
