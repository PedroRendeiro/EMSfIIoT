using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mime;
using EMSfIIoT_API.DbContexts;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Security.Claims;
using System.Runtime.InteropServices.ComTypes;
using System.ComponentModel.DataAnnotations;

namespace EMSfIIoT_API.Controllers
{
    /// <summary>Manage your notifications</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Consumes(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    public class NotificationsController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public NotificationsController(ApiDbContext context)
        {
            _context = context;
        }

        /// <summary>Get all your database entries</summary>
        /// <remarks></remarks>
        /// <param name="limit" example="0">Values to be retured</param>
        /// <param name="skip" example="0">Number on entries to be skiped</param>
        /// <param name="filter" example="">Filter the notification query results
        /// <h4>Filter:</h4>
        /// <code>read | unread</code>
        /// </param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        [HttpGet]
        [Authorize(Policy = "read")]
        public async Task<ActionResult<IEnumerable<Notification>>> GetNotifications([FromQuery]string? filter = "", [FromQuery]int? limit = 0, [FromQuery]int? skip = 0)
        {
            int limitValue = limit.Value;
            if (limitValue < 0)
            {
                return BadRequest(new Response
                {
                    StatusCode = 400,
                    Error = "Bad Request",
                    Message = "Limit not valid",
                    Description = "Limit " + limitValue.ToString() + " supplied isn't valid in the current context."
                });
            }
            int skipValue = skip.Value;
            if (skipValue < 0)
            {
                return BadRequest(new Response
                {
                    StatusCode = 400,
                    Error = "Bad Request",
                    Message = "Skip not valid",
                    Description = "Skip " + skipValue.ToString() + " supplied isn't valid in the current context."
                });
            }

            IQueryable<Notification> result = _context.Notifications.Where(notification => notification.Destination.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value));
            
            switch (filter.ToLower().Trim())
            {
                case "":
                        break;
                case "read":
                    result = result.Where(notification => notification.ReadAt.HasValue);
                    break;
                case "unread":
                    result = result.Where(notification => !notification.ReadAt.HasValue);
                    break;
                default:
                    return BadRequest();
            }

            if (limitValue != 0)
            {
                result = result.Take(limitValue);
            }

            return await result
                .OrderByDescending(a => a.Id)
                .Skip(skipValue)
                .ToListAsync();
        }

        /// <summary>Get a database entry with specified Id</summary>
        /// <param name="id" example="5">The entry id to get</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Entry not found</response>
        [HttpGet("{id}")]
        [Authorize(Policy = "read")]
        public async Task<ActionResult<Notification>> GetNotification([FromRoute, Required]long id)
        {
            var notification = await _context.Notifications.FindAsync(id);

            if (notification == null)
            {
                return NotFound();
            }
            else if (!notification.Destination.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            return notification;
        }

        /// <summary>Update a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to update</param>
        /// <param name="notificationDto" example="">Entry data</param>
        /// <response code="204">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> PutNotifications([FromRoute, Required]long id, [FromBody, BindRequired]NotificationDTO notificationDto)
        {
            if (!NotificationsExists(id))
            {
                return NotFound();
            }

            Notification notification = await _context.Notifications.AsNoTracking().Where(e => e.Id.Equals(id)).FirstAsync();
            if (!notification.Origin.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            notification = new Notification
            {
                Id = id,
                Type = notificationDto.Type,
                Title = notificationDto.Title,
                Description = notificationDto.Description,
                Origin = notificationDto.Origin,
                Destination = new Guid(notificationDto.Destination)
            };

            _context.Entry(notification).State = EntityState.Modified;

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
        /// <param name="notificationDTO">Entry data</param>
        /// <response code="201">Entry created successfully</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Authorize(Policy = "write")]
        public async Task<ActionResult<Notification>> PostNotifications([FromBody, BindRequired]NotificationDTO notificationDTO)
        {
            if (Request != null)
            {
                if (!notificationDTO.Origin.Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                {
                    return StatusCode(403);
                }
            }

            Notification notification = new Notification
            {
                Type = notificationDTO.Type,
                Title = notificationDTO.Title,
                Description = notificationDTO.Description,
                Origin = notificationDTO.Origin,
                Destination = new Guid(notificationDTO.Destination)
            };
            
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetNotification", new { id = notification.Id }, notification);
        }

        /// <summary>Delete a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to delete</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "write")]
        public async Task<ActionResult<Notification>> DeleteNotifications([FromRoute, Required]long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }
            else if (!notification.Destination.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            {
                return StatusCode(403);
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return notification;
        }

        /// <summary>Mark your notifications as read</summary>
        /// <response code="204">Successful query</response>
        [HttpPatch]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> UserNotificationsMarkRead()
        {
            var notifications = await _context.Notifications
            .Where(notification => notification.Destination.ToString().Equals(User.FindFirst(ClaimTypes.NameIdentifier).Value) & !notification.ReadAt.HasValue)
            .ToListAsync();
            notifications.ForEach(notification => notification.ReadAt = DateTime.Now);

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

        private bool NotificationsExists(long id)
        {
            return _context.Notifications.Any(e => e.Id == id);
        }
    }
}
