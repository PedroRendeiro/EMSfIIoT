using System;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

using EMSfIIoT_API.Models;
using System.ComponentModel.DataAnnotations;

namespace EMSfIIoT_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerTag("Manage your gateways")]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class GatewaysController : ControllerBase
    {

        /*private readonly ILogger<GatewaysController> _logger;
        private readonly IWebHostEnvironment hostingEnvironment;

        public GatewaysController(ILogger<GatewaysController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            hostingEnvironment = environment;
        }

        [HttpGet]
        [SwaggerOperation("Get all your registered gateways")]
        public IEnumerable<Gateway> GetAll([FromQuery]int? limit = 5, [FromQuery]int? skip = 0)
        {
            return Enumerable.Range(skip.Value + 1, limit.Value).Select(index => new Gateway
            {
                Date = DateTime.Now.AddDays(-index),
                Uri = IPAddress.Parse("43.77.248.176").ToString(),
                Id = Guid.NewGuid()
            })
            .ToArray();
        }

        [HttpPost]
        [ProducesResponseType(typeof(Gateway), 200)]
        [ProducesResponseType(typeof(EmptyResult), 400)]
        [SwaggerOperation("Register a new gateway")]
        public ActionResult<Gateway> Post([FromBody, BindRequired] Gateway gateway)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            } else
            {
                return Ok(new Gateway
                {
                    Date = DateTime.Now,
                    Uri = gateway.Uri,
                    Id = Guid.NewGuid()
                });
            }
        }

        [HttpGet]
        [Route("{gatewayId:guid}")]
        [SwaggerOperation("Get a registered gateway with specified Id")]
        public Gateway GetById([FromRoute, BindRequired]Guid? gatewayId = null)
        {
            return new Gateway
            {
                Date = DateTime.Now.AddDays(-1),
                Uri = IPAddress.Parse("1").ToString(),
                Id = gatewayId.Value
            };
        }

        [HttpPut]
        [Route("{gatewayId:guid}")]
        [SwaggerOperation("Register or update a gateway with specified Id")]
        public Gateway Put([FromBody, BindRequired] Gateway gateway, [FromRoute, BindRequired]Guid? gatewayId = null)
        {
            return new Gateway
            {
                Date = DateTime.Now.AddDays(-1),
                Uri = IPAddress.Parse("43.77.248.176").ToString(),
                Id = gatewayId.Value
            };
        }

        [HttpDelete]
        [Route("{gatewayId:guid}")]
        [SwaggerOperation("Delete a registered gateway with specified Id")]
        public ActionResult Delete([FromRoute, BindRequired]Guid? gatewayId = null)
        {
            return Ok("Successfully deleted gateway with Id " + gatewayId.ToString());
        }

        /// <summary>Update gateway CNN model</summary>
        /// <remarks>
        /// Upload a file to update specified gateway Keras model.<br/><br/>
        /// Max upload size for <code>model</code> is 134 217 728 Bytes.
        /// </remarks>
        /// <param name="model">Keras model file to update</param>
        /// <param name="gatewayId">Guid of the gateway to be updated</param>
        /// <response code="200">Successful update</response>
        /// <response code="400">Invalid file</response>
        [HttpPost, DisableRequestSizeLimit]
        [Route("{gatewayId:guid}/settings")]
        [ProducesResponseType(typeof(Device), 200)]
        public async Task<IActionResult> UpdateCNNModel([Required]IFormFile model, [FromRoute, BindRequired]Guid? gatewayId)
        {           
            var size = model.Length;
            var contentType = model.ContentType;
            var fileName = gatewayId.ToString() + "." + Path.GetRandomFileName() + ".h5";

            var filePath = Path.Combine("wwwroot/KerasModels/", Path.GetFileName(fileName));

            if (size > 0 & size < 134217728)
            {
                var stream = System.IO.File.Create(filePath);
                await model.CopyToAsync(stream);

                return Ok(new { fileName, contentType, size });
            }
            else
            {
                return BadRequest(new Response
                {
                    StatusCode = 400,
                    Error = "Bad Request",
                    Message = "Couldn't upload file",
                    Description = "No file was uploaded!"
                });
            }
        }
        */
    }
}