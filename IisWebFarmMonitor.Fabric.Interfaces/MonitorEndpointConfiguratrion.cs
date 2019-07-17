using System;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    public class MonitorEndpointConfiguratrion
    {

        /// <summary>
        /// Name of the remote IIS server.
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Name of the web farm server to be configured.
        /// </summary>
        public string ServerFarmName { get; set; }

        /// <summary>
        /// Time between refreshes.
        /// </summary>
        public TimeSpan? Interval { get; set; }

    }

}