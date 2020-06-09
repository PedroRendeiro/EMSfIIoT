using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Net.Mime;
using EMSfIIoT_API.DbContexts;
using SharedAPI;

/*

namespace EMSfIIoT_API.Controllers
{
    /// <summary>Manage your measure database</summary>
    [ApiController]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    public class MeasuresController : ControllerBase
    {
        private readonly ApiDbContext _context;

        public MeasuresController(ApiDbContext context)
        {
            _context = context;
        }

        private bool MeasureExists(long id)
        {
            return _context.Measure_API.Any(e => e.Id == id);
        }

        /// <summary>Get all your database entries</summary>
        /// <remarks>
        /// Limited, with parameter <code>limit</code> to 255 entries at a time<br/><br/>
        /// Ordered from newest to oldest
        /// </remarks>
        /// <param name="dateTime" example="2020-04-22T14:28:16"></param>
        /// <param name="limit" example="0">Values to be retured</param>
        /// <param name="skip" example="0">Number on entries to be skiped</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        [HttpGet]
        [Authorize(Policy = "read")]
        public ActionResult<IEnumerable<Measure>> GetMeasures([FromQuery]DateTime? dateTime, [FromQuery]int? limit = 0, [FromQuery]int? skip = 0)
        {
            var limitValue = limit.Value;
            var skipValue = skip.Value;

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

            List<Gateway> gateways = BoschIoTSuiteApiConnector.GetUserDevices(HttpContext);

            List<Measure> measures = new List<Measure>();

            gateways.ForEach(gateway =>
            {
                gateway.attributes.configuration.devices.ForEach(device =>
                {
                    var x = _context.Measure_API
                        .Where(measure => measure.LocationID.Equals(device.locationId));
                    if (dateTime.HasValue)
                    {
                        x = x.Where(measure => measure.TimeStamp >= dateTime.Value);
                    }
                    measures.AddRange(x.ToList());
                });
            });

            if (limitValue == 0)
            {
                measures = measures
               .OrderByDescending(measure => measure.Id)
               .Skip(skipValue)
               .ToList();
            }
            else
            {
                measures = measures
               .OrderByDescending(measure => measure.Id)
               .Skip(skipValue)
               .Take(limitValue)
               .ToList();
            }

            return measures;
        }

        /// <summary>Get a database entry with specified Id</summary>
        /// <param name="id" example="5">The entry id to get</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Entry not found</response>
        [HttpGet("{id}")]
        [Authorize(Policy = "read")]
        public async Task<ActionResult<Measure>> GetMeasure([FromRoute, Required] long id)
        {            
            var measure = await _context.Measure_API.FindAsync(id);

            if (measure == null)
            {
                return NotFound(new Response
                {
                    StatusCode = 404,
                    Error = "Not Found",
                    Message = "Id doesn't exist in the database",
                    Description = "Entry with Id " + id.ToString() + " doesn't exist."
                });
            }

            return measure;
        }

        /// <summary>Update a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to update</param>
        /// <param name="measureDTO" example="123">Entry data</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpPut("{id}")]
        [Authorize(Policy = "write")]
        public async Task<IActionResult> PutMeasure([FromRoute, Required] long id, [FromBody, Required] MeasureDTO measureDTO)
        {
            Measure measure = new Measure
            {
                Id = id,
                MeasureTypeID = measureDTO.MeasureTypeID,
                LocationID = measureDTO.LocationID,
                Value = measureDTO.Value,
                Unit = measureDTO.Unit
            };

            _context.Entry(measure).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeasureExists(id))
                {
                    return NotFound(new Response
                    {
                        StatusCode = 404,
                        Error = "Not Found",
                        Message = "Id doesn't exist in the database",
                        Description = "Entry with Id " + id.ToString() + " doesn't exist."
                    });
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        /// <summary>Create a new database entry</summary>
        /// <param name="measureDTO">Entry data</param>
        /// <response code="201">Entry created successfully</response>
        /// <response code="400">Bad Request</response>
        [HttpPost]
        [Authorize(Policy = "write")]
        [ProducesResponseType(typeof(Measure), 201)]
        public async Task<ActionResult<Measure>> PostMeasure([FromBody, BindRequired] MeasureDTO measureDTO)
        {
            Measure measure = new Measure
            {
                MeasureTypeID = measureDTO.MeasureTypeID,
                LocationID = measureDTO.LocationID,
                Value = measureDTO.Value,
                Unit = measureDTO.Unit
            };

            _context.Measure_API.Add(measure);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetMeasure", new { id = measure.Id }, measure);
        }

        /// <summary>Delete a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to delete</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpDelete("{id}")]
        [Authorize(Policy = "write")]
        public async Task<ActionResult<Measure>> DeleteMeasure([FromRoute, Required] long id)
        {
            var measure = await _context.Measure_API.FindAsync(id);
            if (measure == null)
            {
                return NotFound(new Response
                {
                    StatusCode = 404,
                    Error = "Not Found",
                    Message = "Id doesn't exist in the database",
                    Description = "Entry with Id " + id.ToString() + " doesn't exist."
                });
            }

            _context.Measure_API.Remove(measure);
            await _context.SaveChangesAsync();

            return measure;
        }
    }
}
*/