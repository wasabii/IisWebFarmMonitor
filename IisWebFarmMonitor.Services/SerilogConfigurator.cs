using Cogito.Autofac;
using Cogito.Serilog;

using Serilog;

namespace IisWebFarmMonitor.Services
{

    [RegisterAs(typeof(ILoggerConfigurator))]
    public class SerilogConfigurator : ILoggerConfigurator
    {

        public LoggerConfiguration Apply(LoggerConfiguration configuration)
        {
            return configuration
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithExceptionLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithMachineName()
                .Enrich.WithMemoryUsage()
                .Enrich.WithProcessId()
                .Enrich.WithProcessName();
        }

    }

}
