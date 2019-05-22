using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading.Tasks;

using Cogito.Collections;

using IisWebFarmMonitor.Fabric.Interfaces;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.Web.Administration;

using Newtonsoft.Json;
using Serilog;

namespace IisWebFarmMonitor.Services
{

    [StatePersistence(StatePersistence.Persisted)]
    public class MonitorActor : Microsoft.ServiceFabric.Actors.Runtime.Actor, IMonitorActor, IRemindable
    {

        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="actorService"></param>
        /// <param name="actorId"></param>
        /// <param name="logger"></param>
        public MonitorActor(ActorService actorService, ActorId actorId, ILogger logger) :
            base(actorService, actorId)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<MonitorConfiguration> GetConfig()
        {
            var a = await StateManager.TryGetStateAsync<MonitorConfiguration>("Configuration");
            return a.HasValue ? a.Value : null;
        }

        public async Task<MonitorConfiguration> SetConfig(MonitorConfiguration config)
        {
            await StateManager.SetStateAsync("Configuration", config);
            await ConfigChanged(config);
            return await GetConfig();
        }

        /// <summary>
        /// Invoked when the configuration is changed.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        async Task ConfigChanged(MonitorConfiguration config)
        {
            try
            {
                var existing = GetReminder("Interval");
                if (existing != null)
                    await UnregisterReminderAsync(existing);
            }
            catch
            {
                // no big deal
            }

            if (config != null)
            {
                await RegisterReminderAsync(
                    "Interval",
                    null,
                    TimeSpan.FromSeconds(1),
                    config.Interval ?? TimeSpan.FromSeconds(30));
            }
        }

        /// <summary>
        /// Invoked when a reminder is triggered.
        /// </summary>
        /// <param name="reminderName"></param>
        /// <param name="state"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            try
            {
                var config = await GetConfig();
                if (config == null)
                {
                    await ConfigChanged(null);
                    return;
                }

                if (string.IsNullOrWhiteSpace(config.IisServerName))
                    throw new InvalidOperationException("Missing IisServerName configuration.");

                if (string.IsNullOrWhiteSpace(config.WebServerName))
                    throw new InvalidOperationException("Missing WebServerName configuration.");

                var endpoints = await GetServiceEndpointsAsync(config);
                if (endpoints == null)
                    return;

                await Task.Run(() =>
                {
                    var serverManager = ServerManager.OpenRemote(config.IisServerName);
                    if (serverManager == null)
                        throw new InvalidOperationException("Null reference attempting to open IIS server manager.");

                    var serverConfig = serverManager.GetApplicationHostConfiguration();
                    if (serverConfig == null)
                        throw new InvalidOperationException("Null reference obtaining application host configuration.");

                    logger.Debug("Set config here.");
                });
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception setting remote IIS server farm.");
            }
        }

        /// <summary>
        /// Returns the set of all endpoint URIs being listened to for the specified configuration.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        async Task<IEnumerable<Uri>> GetServiceEndpointsAsync(MonitorConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var endpoints = new List<Uri>();

            using (var client = new FabricClient(FabricClientRole.User))
            {
                var service = await client.ServiceManager.GetServiceDescriptionAsync(new Uri(Id.GetStringId()));
                if (service == null)
                    return null;

                var partition = await client.ServiceManager.ResolveServicePartitionAsync(service.ServiceName);
                if (partition == null)
                    return null;

                foreach (var endpoint in GetEndpoints(partition.Endpoints, config))
                    endpoints.Add(endpoint);

                return endpoints;
            }
        }

        /// <summary>
        /// Returns the URLs from the given service endpoints.
        /// </summary>
        /// <param name="serviceEndpoints"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        IEnumerable<Uri> GetEndpoints(IEnumerable<ResolvedServiceEndpoint> serviceEndpoints, MonitorConfiguration config)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException(nameof(serviceEndpoints));

            foreach (var serviceEndpoint in serviceEndpoints)
            {
                var address = JsonConvert.DeserializeObject<Address>(serviceEndpoint.Address);
                if (address == null)
                    continue;

                var endpointAddress = address.Endpoints.GetOrDefault(config.ServiceEndpointName ?? address.Endpoints.Keys.FirstOrDefault());
                if (endpointAddress != null)
                    if (endpointAddress.Scheme == "http" || endpointAddress.Scheme == "https")
                        yield return endpointAddress;
            }
        }

        class Address
        {

            public Dictionary<string, Uri> Endpoints { get; set; }

        }

    }

}
