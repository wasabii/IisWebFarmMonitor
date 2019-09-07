using System.Threading;
using System.Threading.Tasks;

using Autofac;
using Autofac.Integration.ServiceFabric;

using Cogito.Autofac;
using Cogito.Autofac.DependencyInjection;
using Cogito.ServiceFabric.AspNetCore.Kestrel.Autofac;

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
        public static async Task Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();
            builder.RegisterServiceFabricSupport();
            builder.RegisterStatelessService<MonitorService>("MonitorServiceType");
            builder.RegisterStatelessKestrelWebService<WebService>("WebServiceType");
            builder.RegisterActor<MonitorActor>(typeof(MonitorActorService));
            builder.Populate(s => s.AddLogging());

            using (builder.Build())
                await Task.Delay(Timeout.Infinite);
        }

    }

}
