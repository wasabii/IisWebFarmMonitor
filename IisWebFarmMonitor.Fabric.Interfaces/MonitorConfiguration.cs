using System.Collections.Generic;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    public class MonitorConfiguration
    {

        /// <summary>
        /// Optional name of the service endpoint to push.
        /// </summary>
        public Dictionary<string, MonitorEndpointConfiguratrion> Endpoints { get; set; }

    }

}
