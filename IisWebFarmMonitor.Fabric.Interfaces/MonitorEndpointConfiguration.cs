using System;
using System.Runtime.Serialization;

using Newtonsoft.Json;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    [DataContract]
    public class MonitorEndpointConfiguration
    {

        /// <summary>
        /// Name of the remote IIS server.
        /// </summary>
        [JsonProperty("ServerName")]
        [DataMember]
        public string ServerName { get; set; }

        /// <summary>
        /// Name of the web farm server to be configured.
        /// </summary>
        [JsonProperty("ServerFarmName")]
        [DataMember]
        public string ServerFarmName { get; set; }

        /// <summary>
        /// Time between refreshes.
        /// </summary>
        [JsonProperty("Interval")]
        [DataMember]
        public TimeSpan? Interval { get; set; }

    }

}