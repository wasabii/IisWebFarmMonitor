using System;
using System.Threading;
using System.Threading.Tasks;

using IisWebFarmMonitor.Fabric.Interfaces;

using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;

using Serilog;

namespace IisWebFarmMonitor.Services
{

    [Route("monitors")]
    public class MonitorController : Controller
    {

        readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="logger"></param>
        public MonitorController(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [HttpGet("{*serviceName}")]
        public async Task<IActionResult> GetMonitor(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest();

            var n = new Uri("fabric:/" + serviceName);
            var p = ActorProxy.Create<IMonitorActor>(new ActorId(n.ToString()));
            return Ok(await p.GetConfig());
        }

        [HttpPut("{*serviceName}")]
        public async Task<IActionResult> SetMonitor(string serviceName, [FromBody] MonitorConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest();

            var n = new Uri("fabric:/" + serviceName);
            var p = ActorProxy.Create<IMonitorActor>(new ActorId(n.ToString()));
            return Ok(await p.SetConfig(config));
        }

        [HttpDelete("{*serviceName}")]
        public async Task<IActionResult> DeleteMonitor(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
                return BadRequest();

            var n = new Uri("fabric:/" + serviceName);
            var p = ActorProxy.Create<IMonitorActor>(new ActorId(n.ToString()));
            var s = ActorServiceProxy.Create(p.GetActorReference().ServiceUri, p.GetActorId());
            await s.DeleteActorAsync(p.GetActorId(), CancellationToken.None);
            return Ok();
        }

    }

}
