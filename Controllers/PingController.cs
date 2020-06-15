using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MessageBroker.Model;
using Microsoft.AspNetCore.Mvc;

namespace MessageBroker.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PingController : ControllerBase
    {
        // GET: <PingController>
        [HttpGet]
        public string Get()
        {
            return "Pong";
        }
    }
}
