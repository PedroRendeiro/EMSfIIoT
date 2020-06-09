using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;

using Measures_API.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Net.Mime;  

namespace Measures_API.Controllers
{
    /// <summary>Manage your measure database</summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
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
        /// <param name="locationId" example="1"></param>
        /// <param name="measureTypeID" example="1"></param>
        /// <param name="unit" example="kwh"></param>
        /// <param name="dateTime" example="2020-04-22T14:28:16"></param>
        /// <param name="limit" example="0">Values to be retured</param>
        /// <param name="skip" example="0">Number on entries to be skiped</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        [HttpGet]
        public ActionResult<IEnumerable<Measure>> GetMeasures(
            [FromQuery]long? locationId,
            [FromQuery]int? measureTypeID,
            #nullable enable
            [FromQuery]string? unit,
            #nullable disable
            [FromQuery]DateTime? dateTime,
            [FromQuery]int? limit = 0,
            [FromQuery]int? skip = 0
        )
        {
            var limitValue = limit.Value;
            var skipValue = skip.Value;

            IOrderedQueryable<Measure> query = _context.Measure_API.OrderByDescending(measure => measure.Id);
            List<Measure> measures;
            
            if(dateTime.HasValue)
            {
                query = query
                    .Where(measure => measure.TimeStamp >= dateTime.Value)
                    .OrderByDescending(measure => measure.Id);
            }
            
            if(locationId.HasValue)
            {
                query = query
                    .Where(measure => measure.LocationID.Equals(locationId))
                    .OrderByDescending(measure => measure.Id);
            }

            if (measureTypeID.HasValue)
            {
                query = query
                    .Where(measure => measure.MeasureTypeID.Equals(measureTypeID))
                    .OrderByDescending(measure => measure.Id);
            }

            if (unit != null)
            {
                query = query
                    .Where(measure => measure.Unit.Equals(unit))
                    .OrderByDescending(measure => measure.Id);
            }

            if (limitValue == 0)
            {
                measures = query.Skip(skipValue).ToList();
            }
            else
            {
                measures = query.Skip(skipValue).Take(limitValue).ToList();
            }

            return measures;
        }

        /// <summary>Get a database entry with specified Id</summary>
        /// <param name="id" example="5">The entry id to get</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Entry not found</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<Measure>> GetMeasure([FromRoute, Required] long id)
        {
            var measure = await _context.Measure_API.FindAsync(id);

            if (measure == null)
            {
                return NotFound();
            }

            return measure;
        }

        /// <summary>Update a database entry with specified Id</summary>
        /// <param name="id" example="50">The entry id to update</param>
        /// <param name="measureDTO" example="123">Entry data</param>
        /// <response code="200">Successful query</response>
        /// <response code="404">Entry not found</response>
        [HttpPut("{id}")]
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
                    return NotFound();
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
        public async Task<ActionResult<Measure>> DeleteMeasure([FromRoute, Required] long id)
        {
            var measure = await _context.Measure_API.FindAsync(id);
            if (measure == null)
            {
                return NotFound();
            }

            _context.Measure_API.Remove(measure);
            await _context.SaveChangesAsync();

            return measure;
        }
    }
}