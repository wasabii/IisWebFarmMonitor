using System.Threading;

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
        public static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterAllAssemblyModules();
            builder.RegisterServiceFabricSupport();
            builder.RegisterStatelessKestrelWebService<WebService>("WebServiceType");
            builder.RegisterActor<MonitorActor>();
            builder.Populate(s => s.AddLogging());

            using (builder.Build())
                Thread.Sleep(Timeout.Infinite);
        }

    }

}
