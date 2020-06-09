using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mime;
using System.Net;
using Microsoft.AspNetCore.Http;
using Measures_API.Models;

namespace Measures_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class BoschIoTSuiteController : ControllerBase
    {
        private readonly ApiDbContext _context;
        private readonly MeasuresController _measures;

        public BoschIoTSuiteController(ApiDbContext context)
        {
            _context = context;
            _measures = new MeasuresController(_context);
        }

        // POST: BoschIoTSuite/telemetry/gatewayId
        /// <summary>Create a new database entry from Bosch IoT Suite POST</summary>
        /// <param name="body">Entry data</param>
        /// <param name="gatewayId">Entry data</param>
        /// <response code="201">Entry created successfully</response>
        /// <response code="400">Bad Request</response>
        [HttpPost("telemetry/{gatewayId}")]
        [ProducesResponseType(typeof(Measure), 201)]        
        public IActionResult PostTelemetry([FromBody, BindRequired] BoschIoTSuiteTelmetry body, [FromRoute, BindRequired]string gatewayId = null)
        {
            var measure = _measures.PostMeasure(body.Value.Status).Result;

            return new ObjectResult(measure.Value) { StatusCode = StatusCodes.Status201Created };
        }
    }
}