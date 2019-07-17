using System;

using Cogito.Autofac;
using Cogito.Serilog;

using IisWebFarmMonitor.Services.Configuration;

using Serilog;

namespace IisWebFarmMonitor.Services
{

    [RegisterAs(typeof(ILoggerConfigurator))]
    class SerilogSeqConfigurator : ILoggerConfigurator
    {

        readonly SeqOptions options;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="options"></param>
        public SerilogSeqConfigurator(SeqOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public LoggerConfiguration Apply(LoggerConfiguration configuration)
        {
            if (options != null && options.ServerUrl != null && options.ApiKey != null)
                return configuration.WriteTo.Seq(options.ServerUrl, apiKey: options.ApiKey);

            return configuration;
        }

    }

}
