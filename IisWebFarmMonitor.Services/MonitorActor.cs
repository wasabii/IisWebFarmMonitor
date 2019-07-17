using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Text;
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
    public class MonitorActor : Actor, IMonitorActor, IRemindable
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
            logger.Information("Configuration for {ServiceName} changed to {@Config}.", this.GetActorId().GetStringId(), config);

            var endpoints = await GetServiceEndpointsAsync(config);
            if (endpoints == null)
                throw new InvalidOperationException("Unable to obtain service endpoints.");

            // unregister all known endpoint reminders
            foreach (var endpoint in endpoints)
            {
                try
                {
                    var existing = GetReminder("Reminder_" + endpoint.Name);
                    if (existing != null)
                        await UnregisterReminderAsync(existing);
                }
                catch
                {
                    // no big deal
                }
            }

            // register new reminders
            if (config != null && config.Endpoints != null)
            {
                foreach (var endpoint in endpoints)
                {
                    var c = config.Endpoints.GetOrDefault(endpoint.Name);
                    if (c != null)
                        await RegisterReminderAsync(
                            "Reminder_" + endpoint.Name,
                            Encoding.UTF8.GetBytes(endpoint.Name),
                            TimeSpan.FromSeconds(1),
                            c.Interval ?? TimeSpan.FromSeconds(30));
                }
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
            logger.Information("Attempting to push server farm settings for {WebFarmName}.", this.GetActorId().GetStringId());

            try
            {
                var config = await GetConfig();
                if (config == null)
                {
                    await ConfigChanged(null);
                    return;
                }

                // old versions may have bad state
                if (state == null)
                {
                    logger.Warning("Missing state for {Reminder}. Please reconfigure service.", reminderName);
                    return;
                }

                // reminder is fired for a specific endpoint
                var endpointName = Encoding.UTF8.GetString(state);
                if (endpointName == null)
                    return;

                // find configuration for endpoint
                var endpointConfig = config.Endpoints?.GetOrDefault(endpointName);
                if (endpointConfig == null)
                {
                    logger.Warning("No configuration for {EndpointName}.", endpointName);
                    return;
                }

                if (string.IsNullOrWhiteSpace(endpointConfig.ServerName))
                {
                    logger.Error("Missing ServerName configuration for {EndpointName}.", endpointName);
                    return;
                }

                if (string.IsNullOrWhiteSpace(endpointConfig.ServerFarmName))
                {
                    logger.Error("Missing ServerFarmName configuration for {EndpointName}.", endpointName);
                    return;
                }

                var endpoints = (await GetServiceEndpointsAsync(config)).Where(i => i.Name == endpointName);
                if (endpoints == null)
                {
                    logger.Error("Unable to obtain service endpoints for {EndpointName}.", endpointName);
                    return;
                }

                await Task.Run(() =>
                {
                    using (var serverManager = ServerManager.OpenRemote(endpointConfig.ServerName))
                    {
                        var applicationHostConfig = serverManager.GetApplicationHostConfiguration();
                        if (applicationHostConfig == null)
                            return;

                        var webFarms = applicationHostConfig.GetSection("webFarms")?.GetCollection();
                        if (webFarms == null)
                            return;

                        var webFarm = webFarms.FirstOrDefault(i => (string)i.GetAttributeValue("name") == endpointConfig.ServerFarmName);
                        if (webFarm == null)
                            return;

                        var servers = webFarm.GetCollection()?.ToDictionary(i => (string)i.GetAttributeValue("address"));
                        if (servers == null)
                            return;

                        foreach (var endpoint in endpoints)
                        {
                            logger.Debug("Checking server {ServerName}.", endpoint.Address.Host);

                            // find or create server reference
                            var server = servers.GetOrDefault(endpoint.Address.Host);
                            if (server == null)
                            {
                                logger.Information("Adding server {ServerName}.", endpoint.Address.Host);

                                server = webFarm.GetCollection().CreateElement("server");
                                server.SetAttributeValue("address", endpoint.Address.Host);
                                webFarm.GetCollection().Add(server);
                            }

                            // endpoint port or defaults
                            var httpPort = endpoint.Address.Scheme == "http" ? endpoint.Address.Port : 80;
                            var httpsPort = endpoint.Address.Scheme == "https" ? endpoint.Address.Port : 443;

                            // update server settings
                            if (server.GetAttributeValue("address")?.ToString() != endpoint.Address.Host ||
                                server.GetAttributeValue("httpPort")?.ToString() != httpPort.ToString() ||
                                server.GetAttributeValue("httpsPort")?.ToString() != httpsPort.ToString())
                            {
                                logger.Information("Updating {ServerName} to {HttpPort}/{HttpsPort}.", endpoint.Address.Host, httpPort, httpsPort);

                                var arr = server.GetChildElement("applicationRequestRouting");
                                arr.SetAttributeValue("httpPort", httpPort.ToString());
                                arr.SetAttributeValue("httpsPort", httpsPort.ToString());
                            }

                            // remove from dictionary, signifies completion
                            servers.Remove(endpoint.Address.Host);
                        }

                        // remove remaining servers
                        foreach (var server in servers)
                        {
                            logger.Information("Removing server {ServerName}.", server.Key);
                            server.Value.Delete();
                        }

                        // save changes
                        serverManager.CommitChanges();
                    }
                });
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception attempting to update web farm.");
            }
        }

        /// <summary>
        /// Returns the set of all endpoint URIs being listened to for the specified configuration.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        async Task<IEnumerable<(string Name, Uri Address)>> GetServiceEndpointsAsync(MonitorConfiguration config)
        {
            var endpoints = new List<(string, Uri)>();

            using (var client = new FabricClient(FabricClientRole.User))
            {
                var service = await client.ServiceManager.GetServiceDescriptionAsync(new Uri(Id.GetStringId()));
                if (service == null)
                    return null;

                var partition = await client.ServiceManager.ResolveServicePartitionAsync(service.ServiceName);
                if (partition == null)
                    return null;

                foreach (var endpoint in GetEndpoints(partition.Endpoints))
                    endpoints.Add(endpoint);

                return endpoints;
            }
        }

        /// <summary>
        /// Returns the URLs for the given service endpoints.
        /// </summary>
        /// <param name="serviceEndpoints"></param>
        /// <returns></returns>
        IEnumerable<(string, Uri)> GetEndpoints(IEnumerable<ResolvedServiceEndpoint> serviceEndpoints)
        {
            if (serviceEndpoints == null)
                throw new ArgumentNullException(nameof(serviceEndpoints));

            foreach (var serviceEndpoint in serviceEndpoints)
            {
                var address = JsonConvert.DeserializeObject<Address>(serviceEndpoint.Address);
                if (address == null)
                    continue;

                foreach (var endpoint in address.Endpoints)
                    if (endpoint.Value.Scheme == "http" || endpoint.Value.Scheme == "https")
                        yield return (endpoint.Key, endpoint.Value);
            }
        }

        /// <summary>
        /// Describes the service endpoint address data.
        /// </summary>
        class Address
        {

            public Dictionary<string, Uri> Endpoints { get; set; }

        }

    }

}
