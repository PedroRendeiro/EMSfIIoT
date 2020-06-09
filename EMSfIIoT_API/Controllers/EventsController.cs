using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMSfIIoT_API.DbContexts;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using EMSfIIoT_API.Tasks;

namespace EMSfIIoT_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    public class EventsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public EventsController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>Get all your database entries</summary>
        /// <remarks></remarks>
        /// <response code="200">Successful query</response>
        [HttpGet]
        [Authorize(Policy = "read")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvent()
        {
            return await _context.Event
                .Where(@event => @event.Username.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                .ToListAsync();
        }

        /// <summary>Get a database entry with specified Id</summary>
        /// <param name="id" example="5">The entry id to get</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpGet("{id}")]
        [Authorize(Policy = "read")]
        public async Task<ActionResult<Event>> GetEvent([FromRoute, Required]long id)
        {
            var @event = await _context.Event.FindAsync(id);

            if (@event == null)
            {
                return NotFound();
            }
            else if (!@event.Username.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            return @event;
        }

        /// <summary>Update a database entry with specified Id</summary>
        /// <param name="id" example="5">The entry id to update</param>
        /// <param name="eventDto" example="">Entry data</param>
        /// <response code="204">Successful query</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Entry not found</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> PutEvent([FromRoute, Required]long id, [FromBody, BindRequired]EventDTO eventDto)
        {
            if (!EventExists(id))
            {
                return NotFound();
            }

            Event eventEntry = await _context.Event.AsNoTracking().Where(e => e.Id.Equals(id)).FirstAsync();
            if (!eventEntry.Username.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            Event @event = new Event
            {
                Id = id,
                Username = new Guid(User.FindFirst(ClaimTypes.NameIdentifier).Value),
                Variable = eventDto.Variable,
                EventType = eventDto.EventType,
                EventValueType = eventDto.EventValueType,
                EventValue = eventDto.EventValue,
                EventFrequency = eventDto.EventFrequency,
                NotificationType = eventDto.NotificationType,
                OnHold = false
            };

            NotificationTask.UpdateTaskFromSchedule(@event);

            _context.Entry(@event).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        /// <summary>Create a new database entry</summary>
        /// <param name="eventDto">Entry data</param>
        /// <response code="201">Entry created successfully</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Authorize(Policy = "write")]
        public async Task<ActionResult<Event>> PostEvent([FromBody, BindRequired]EventDTO eventDto)
        {
            Event @event = new Event
            {
                Username = new Guid(User.FindFirst(ClaimTypes.NameIdentifier).Value),
                Variable = eventDto.Variable,
                EventType = eventDto.EventType,
                EventValueType = eventDto.EventValueType,
                EventValue = eventDto.EventValue,
                EventFrequency = eventDto.EventFrequency,
                NotificationType = eventDto.NotificationType
            };

            _context.Event.Add(@event);
            await _context.SaveChangesAsync();

            NotificationTask.AddTaskToSchedule(@event);

            return CreatedAtAction("GetEvent", new { id = @event.Id }, @event);
        }

        /// <summary>Delete a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to delete</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "write")]
        public async Task<ActionResult<Event>> DeleteEvent([FromRoute, Required]long id)
        {
            var @event = await _context.Event.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            else if (!@event.Username.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            NotificationTask.RemoveTaskFromSchedule(@event);

            _context.Event.Remove(@event);
            await _context.SaveChangesAsync();

            return @event;
        }

        /// <summary>Put specified event on hold</summary>
        /// <param name="id" example="5">The entry id to update</param>
        /// <response code="204">Successful query</response>
        /// <response code="400">Bad Request</response>
        /// <response code="404">Entry not found</response>
        [HttpPut("{id}/onHold")]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> PutEventOnHold([FromRoute, Required] long id)
        {
            if (!EventExists(id))
            {
                return NotFound();
            }

            Event eventEntry = await _context.Event.AsNoTracking().Where(e => e.Id.Equals(id)).FirstAsync();

            Event @event = new Event
            {
                Id = id,
                Username = eventEntry.Username,
                Variable = eventEntry.Variable,
                EventType = eventEntry.EventType,
                EventValueType = eventEntry.EventValueType,
                EventValue = eventEntry.EventValue,
                EventFrequency = eventEntry.EventFrequency,
                NotificationType = eventEntry.NotificationType,
                OnHold = true
            };

            _context.Entry(@event).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }

            return NoContent();
        }

        private bool EventExists(long id)
        {
            return _context.Event.Any(e => e.Id == id);
        }
    }
}
