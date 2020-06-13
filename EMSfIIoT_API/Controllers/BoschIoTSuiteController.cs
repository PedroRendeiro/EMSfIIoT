using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mime;
using System.Net;
using Microsoft.AspNetCore.Http;
using EMSfIIoT_API.Models;
using EMSfIIoT_API.DbContexts;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using SharedAPI;
using EMSfIIoT_API.Tasks;

namespace EMSfIIoT_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = "basicAuth")]
    public class BoschIoTSuiteController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public BoschIoTSuiteController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>Notify the API that the specified thing generated an Bosch IoT Things Event</summary>
        /// <param name="thingId">Bosch IoT Things ThingId</param>
        /// <response code="200">Successfully updated</response>
        /// <response code="400">Bad Request</response>
        [HttpPost("event/{thingId}")]
        [ProducesResponseType(typeof(Measure), 201)]
        public async Task<ActionResult> PostTelemetry([FromRoute, BindRequired]string thingId)
        {
            Task.Delay(20000).Wait();

            await _context.Event.AsQueryable().Where(@event => @event.Variable.EndsWith(":" + thingId) | @event.Variable.StartsWith(thingId + ":"))
            .ForEachAsync(async @event =>
            {
                if (@event.EventType.Equals(EventType.Threshold))
                {
                    await NotificationTask.Process(@event);
                }

                @event.OnHold = false;
                _context.Entry(@event).State = EntityState.Modified;

                try
                {
                    _context.SaveChanges();
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
            });

            return NoContent();
        }
    }
}