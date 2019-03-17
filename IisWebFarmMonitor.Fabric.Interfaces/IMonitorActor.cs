using System.Threading.Tasks;

using Microsoft.ServiceFabric.Actors;

namespace IisWebFarmMonitor.Fabric.Interfaces
{

    public interface IMonitorActor : IActor
    {

        Task<MonitorConfiguration> GetConfig();

        Task<MonitorConfiguration> SetConfig(MonitorConfiguration config);

    }

}
