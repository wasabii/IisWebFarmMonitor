using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    [DataContract]
    public class MonitorConfiguration
    {

        /// <summary>
        /// Optional name of the service endpoint to push.
        /// </summary>
        [JsonProperty("Endpoints")]
        [DataMember]
        public Dictionary<string, MonitorEndpointConfiguration> Endpoints { get; set; }

    }

}
