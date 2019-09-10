using Cogito.Extensions.Options.ConfigurationExtensions.Autofac;

namespace IisWebFarmMonitor.Services.Configuration
{

    [RegisterOptions("Seq")]
    public class SeqOptions
    {

        public string ServerUrl { get; set; }

        public string ApiKey { get; set; }

    }

}
