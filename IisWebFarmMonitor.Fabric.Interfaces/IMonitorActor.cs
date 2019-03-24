using System.Threading.Tasks;

using Microsoft.ServiceFabric.Actors;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    public interface IMonitorActor : IActor
    {

        /// <summary>
        /// Gets the configuration of a monitor.
        /// </summary>
        /// <returns></returns>
        Task<MonitorConfiguration> GetConfig();

        /// <summary>
        /// Sets the configuration of a monitor.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task<MonitorConfiguration> SetConfig(MonitorConfiguration config);

    }

}
