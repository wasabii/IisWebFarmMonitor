using System;
using System.Threading.Tasks;

using IisWebFarmMonitor.Fabric.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

namespace IisWebFarmMonitor.Services
{

    [Route("monitors")]
    public class MonitorController : Controller
    {

        [HttpGet("{*serviceName}")]
        public async Task<IActionResult> GetConfig(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest();

            var n = new Uri("fabric:/" + serviceName);
            var p = ActorProxy.Create<IMonitorActor>(new ActorId(n.ToString()));
            return Ok(await p.GetConfig());
        }

        [HttpPut("{*serviceName}")]
        public async Task<IActionResult> SetConfig(string serviceName, [FromBody] MonitorConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest();

            var n = new Uri("fabric:/" + serviceName);
            var p = ActorProxy.Create<IMonitorActor>(new ActorId(n.ToString()));
            return Ok(await p.SetConfig(config));
        }

    }

}
