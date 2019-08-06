using System.Fabric;
using Autofac;

using Cogito.Autofac;
using Cogito.Extensions.Configuration.Autofac;

using IisWebFarmMonitor.Services.Configuration;

namespace IisWebFarmMonitor.Services
{

    public class AssemblyModule : ModuleBase
    {

        protected override void Register(ContainerBuilder builder)
        {
            builder.RegisterFromAttributes(typeof(AssemblyModule).Assembly);
            builder.RegisterConfigurationBinding<SeqOptions>("Seq");
            builder.Register(ctx => new FabricClient()).SingleInstance();
        }

    }

}
