using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Autofac;
using Cogito.Collections;

using IisWebFarmMonitor.Fabric.Interfaces;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Runtime;
using Serilog;

namespace IisWebFarmMonitor.Services
{

    /// <summary>
    /// Periodically configures services by registered property information.
    /// </summary>
    [RegisterAs(typeof(MonitorService))]
    public class MonitorService : Microsoft.ServiceFabric.Services.Runtime.StatelessService
    {

        readonly FabricClient fabric;
        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="fabric"></param>
        /// <param name="logger"></param>
        public MonitorService(StatelessServiceContext serviceContext, FabricClient fabric, ILogger logger) :
            base(serviceContext)
        {
            this.fabric = fabric ?? throw new ArgumentNullException(nameof(fabric));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            while (cancellationToken.IsCancellationRequested == false)
            {
                await UpdateMonitors(cancellationToken);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }

        /// <summary>
        /// Updates the monitor configuration based on registered properties.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task UpdateMonitors(CancellationToken cancellationToken)
        {
            foreach (var application in await GetApplications(cancellationToken))
                foreach (var service in await GetServices(application.ApplicationName, cancellationToken))
                    await TryUpdateMonitor(service, cancellationToken);
        }


        /// <summary>
        /// Attempts to update the configuration for the specified service from its available properties.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task TryUpdateMonitor(Service service, CancellationToken cancellationToken)
        {
            try
            {
                await UpdateMonitor(service, cancellationToken);
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Unexpected exception attempting to update {ServiceName}.", service.ServiceName);
            }
        }

        /// <summary>
        /// Updates the configuration for the specified service from its available properties.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="cancellationToken"></param>
        async Task UpdateMonitor(Service service, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var monitor = ActorProxy.Create<IMonitorActor>(new ActorId(service.ServiceName.ToString()));
            var options = await monitor.GetConfig() ?? new MonitorConfiguration();
            var changed = false;

            foreach (var property in await GetNamedPropertiesAsync(service.ServiceName, cancellationToken))
            {
                var key = property.Metadata.PropertyName;
                var val = property.GetValue<string>();
                if (val == null || string.IsNullOrWhiteSpace(val))
                    continue;

                if (Regex.Match(key, @"^IisWebFarmMonitor\.Endpoints\.(\w*)\.(\w+)$") is Match m && m.Success && m.Groups.Count == 3)
                {
                    if (options.Endpoints == null)
                        options.Endpoints = new Dictionary<string, MonitorEndpointConfiguration>();

                    var endpointName = m.Groups[1].Value;
                    var endpoint = options.Endpoints.GetOrAdd(endpointName, _ => new MonitorEndpointConfiguration());
                    if (endpoint == null)
                        throw new InvalidOperationException("Unable to find endpoint configuration.");

                    switch (m.Groups[2].Value)
                    {
                        case nameof(MonitorEndpointConfiguration.ServerName):
                            if (endpoint.ServerName != val)
                            {
                                changed = true;
                                logger.Verbose("Applying ServerName {ServerName} to {ServiceName}:{EndpointName}.", val, service.ServiceName, endpointName);
                                endpoint.ServerName = val;
                            }
                            break;
                        case nameof(MonitorEndpointConfiguration.ServerFarmName):
                            if (endpoint.ServerFarmName != val)
                            {
                                changed = true;
                                logger.Verbose("Applying ServerFarmName {ServerFarmName} to {ServiceName}:{EndpointName}.", val, service.ServiceName, endpointName);
                                endpoint.ServerFarmName = val;
                            }
                            break;
                        case nameof(MonitorEndpointConfiguration.Interval) when TimeSpan.TryParse(val, out var interval):
                            if (endpoint.Interval != interval)
                            {
                                changed = true;
                                logger.Verbose("Applying Interval {Interval} to {ServiceName}:{EndpointName}.", interval, service.ServiceName, endpointName);
                                endpoint.Interval = interval;
                            }
                            break;
                    }
                }
            }

            // save new configuration
            if (changed)
                await monitor.SetConfig(options);
        }

        /// <summary>
        /// Gets all of the existing services.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<IEnumerable<Application>> GetApplications(CancellationToken cancellationToken)
        {
            return await fabric.QueryManager.GetApplicationListAsync();
        }

        /// <summary>
        /// Gets all of the existing services.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<IEnumerable<Service>> GetServices(Uri applicationName, CancellationToken cancellationToken)
        {
            return await fabric.QueryManager.GetServiceListAsync(applicationName);
        }

        /// <summary>
        /// Returns a list of named properties.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async Task<List<NamedProperty>> GetNamedPropertiesAsync(Uri serviceName, CancellationToken cancellationToken)
        {
            var r = new List<NamedProperty>();

            var l = (PropertyEnumerationResult)null;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                l = await fabric.PropertyManager.EnumeratePropertiesAsync(serviceName, true, l);
                r.AddRange(l);
            }
            while (l.HasMoreData);

            return r;
        }

    }

}
