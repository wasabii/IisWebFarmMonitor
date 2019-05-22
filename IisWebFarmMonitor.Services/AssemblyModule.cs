using Autofac;

using Cogito.Autofac;

using IisWebFarmMonitor.Services.Configuration;

namespace IisWebFarmMonitor.Services
{

    public class AssemblyModule : ModuleBase
    {

        protected override void Register(ContainerBuilder builder)
        {
            builder.RegisterFromAttributes(typeof(AssemblyModule).Assembly);
            builder.RegisterConfigurationBinding<SeqOptions>("Seq");
        }

    }

}
