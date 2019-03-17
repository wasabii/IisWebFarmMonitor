using System;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    public class MonitorConfiguration
    {

        /// <summary>
        /// Name of the remote IIS server.
        /// </summary>
        public string IisServerName { get; set; }

        /// <summary>
        /// Name of the web farm server to be configured.
        /// </summary>
        public string WebServerName { get; set; }

        /// <summary>
        /// Optional name of the service endpoint to push.
        /// </summary>
        public string ServiceEndpointName { get; set; }

        /// <summary>
        /// Time between refreshes.
        /// </summary>
        public TimeSpan? Interval { get; set; }

    }

}
