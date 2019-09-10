using System.Fabric;

using Autofac;

using Cogito.Autofac;

namespace IisWebFarmMonitor.Services
{

    public class AssemblyModule : ModuleBase
    {

        protected override void Register(ContainerBuilder builder)
        {
            builder.RegisterFromAttributes(typeof(AssemblyModule).Assembly);
            builder.Register(ctx => new FabricClient()).SingleInstance();
        }

    }

}
