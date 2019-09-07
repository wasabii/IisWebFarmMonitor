﻿using System;
using System.Fabric;
using System.Management;
using System.Threading;
using System.Threading.Tasks;

using Cogito.Autofac;

using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace IisWebFarmMonitor.Services
{

    /// <summary>
    /// Actor service for the <see cref="MonitorActor"/> instances.
    /// </summary>
    [RegisterAs(typeof(MonitorActorService))]
    public class MonitorActorService : ActorService
    {

        readonly FabricClient fabric;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="actorTypeInfo"></param>
        /// <param name="fabric"></param>
        /// <param name="actorFactory"></param>
        /// <param name="stateManagerFactory"></param>
        /// <param name="stateProvider"></param>
        /// <param name="settings"></param>
        public MonitorActorService(
            StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            FabricClient fabric,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null) : base(
                context,
                actorTypeInfo,
                actorFactory,
                stateManagerFactory,
                stateProvider,
                settings)
        {
            this.fabric = fabric ?? throw new ArgumentNullException(nameof(fabric));
        }

        protected override Task RunAsync(CancellationToken cancellationToken)
        {
            // check for existance of IIS before allowing service to run
            if (IsIisManagementAvailable() == false)
                throw new InvalidOperationException("Missing IIS-ManagementConsole Windows Feature. Required for remote IIS management.");

            return base.RunAsync(cancellationToken);
        }

        /// <summary>
        /// Returns <c>true</c> if the IIS management feature is available.
        /// </summary>
        /// <returns></returns>
        bool IsIisManagementAvailable()
        {
            using (var s = new ManagementObjectSearcher("SELECT * FROM Win32_OptionalFeature WHERE Name = 'IIS-ManagementConsole' AND InstallState = 1"))
            using (var l = s.Get())
                foreach (var i in l)
                    return true;

            return false;
        }

    }

}