using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Net;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

using System.Net.Http;
using EMSfIIoT_API.Models;
using System.Text.Json;
using System.Reflection;
using System.Net.Mime;

namespace EMSfIIoT_API.Controllers
{
    [ApiController]
    [Route("Gateways/{gatewayId:guid}/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [SwaggerTag("Manage your devices")]
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DevicesController : ControllerBase
    {

        /*
        private readonly ILogger<GatewaysController> _logger;

        public DevicesController(ILogger<GatewaysController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [SwaggerOperation("Get all your registered devices within specified gateway")]
        public IEnumerable<Device> GetAll([FromRoute, BindRequired]Guid? gatewayId = null, int? limit = 5, int? skip = 0)
        {
            return Enumerable.Range(skip.Value + 1, limit.Value).Select(index => new Device
            {
                Date = DateTime.Now.AddDays(-index),
                Uri = IPAddress.Parse("43.77.248.176").ToString(),
                Id = Guid.NewGuid(),
                Gateway = new Gateway
                {
                    Id = gatewayId.Value
                }
            })
            .ToArray();
        }

        [HttpPost]
        [SwaggerOperation("Register a new device")]
        public Device Post([FromBody, BindRequired] Device device, [FromRoute, BindRequired]Guid? gatewayId = null)
        {
            return new Device
            {
                Date = DateTime.Now,
                Uri = device.Uri,
                Id = device.Id,
                Gateway = new Gateway
                {
                    Id = gatewayId.Value
                }
            };
        }

        [HttpGet]
        [Route("{deviceId:guid}")]
        [SwaggerOperation("Get a registered device with specified Id")]
        public Device GetById([FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid? deviceId = null)
        {
            return new Device
            {
                Date = DateTime.Now,
                Uri = IPAddress.Parse("43.77.248.176").ToString(),
                Id = deviceId.Value,
                Gateway = new Gateway
                {
                    Id = gatewayId.Value
                }
            };
        }

        [HttpPut]
        [Route("{deviceId:guid}")]
        [SwaggerOperation("Register or update a device with specified Id")]
        public Device Put([FromBody, BindRequired] Device device, [FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid ? deviceId = null)
        {
            return new Device
            {
                Date = DateTime.Now.AddDays(-1),
                Uri = IPAddress.Parse("43.77.248.176").ToString(),
                Id = deviceId.Value,
                Gateway = new Gateway
                {
                    Id = gatewayId.Value
                }
            };
        }

        [HttpDelete]
        [Route("{deviceId:guid}")]
        [SwaggerOperation("Delete a registered device with specified Id")]
        public ActionResult Delete([FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid ? deviceId = null)
        {
            return Ok("Successfully deleted device with Id" + deviceId.ToString() + " associated with gateway " + gatewayId.ToString());
        }

        /// <summary>Get device current settings</summary>
        /// <param name="gatewayId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the gateway to witch the device is associated with</param>
        /// <param name="deviceId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the device to query</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Device not found</response>
        [HttpGet]
        [Route("{deviceId:guid}/settings")]
        [ProducesResponseType(typeof(Settings), 200)]
        public async Task<ActionResult> GetSettings([FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid? deviceId = null)
        {
            if (!(deviceId.Value.ToString() == "a2d60823-a247-41f5-a89f-9603853c0001"))
            {
                return NotFound(new Response
                {
                    StatusCode = 404,
                    Error = "Not Found",
                    Message = "Couldn't find device",
                    Description = "Device with Guid " + deviceId.Value.ToString() + " doesn't exist."
                });
            }

            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage message = await client.GetAsync("http://home.pedrorendeiro.eu:8080/status");
                if (message.IsSuccessStatusCode)
                {
                    var resultAsStream = await message.Content.ReadAsStreamAsync();
                    var resultAsSettings = await JsonSerializer.DeserializeAsync<Settings>(resultAsStream,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    return Ok(resultAsSettings);
                }
                else
                {
                    return BadRequest(message.Content);
                }
            }
            catch
            {
                return BadRequest();
            }
        }

        /// <summary>Update device settings</summary>
        /// <param name="gatewayId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the gateway to witch the device is associated with</param>
        /// <param name="deviceId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the device to query</param>
        /// <param name="var" example="led_intensity">Variable to update</param>
        /// <param name="val" example="16">Value to update</param>
        /// <response code="200">Successful query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Device not found</response>
        [HttpPut]
        [Route("{deviceId:guid}/settings")]
        [ProducesResponseType(typeof(Settings), 200)]
        public async Task<ActionResult> PutSettings([FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid? deviceId = null, [FromQuery, BindRequired]String var = null, [FromQuery, BindRequired]String val = null)
        {
            if (!(deviceId.Value.ToString() == "a2d60823-a247-41f5-a89f-9603853c0001"))
            {
                return NotFound(new Response
                {
                    StatusCode = 404,
                    Error = "Not Found",
                    Message = "Couldn't find device",
                    Description = "Device with Guid " + deviceId.Value.ToString() + " doesn't exist."
                });
            }
            
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage message = await client.GetAsync("http://home.pedrorendeiro.eu:8080/control?var=" + var + "&val=" + val);

                if (message.IsSuccessStatusCode)
                {
                    HttpResponseMessage content = await client.GetAsync("http://home.pedrorendeiro.eu:8080/status");
                    if (content.IsSuccessStatusCode)
                    {
                        var resultAsStream = await message.Content.ReadAsStreamAsync();
                        var resultAsSettings = await JsonSerializer.DeserializeAsync<Settings>(resultAsStream,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        return Ok(resultAsSettings);
                    }
                    else
                    {
                        return BadRequest(message.Content);
                    }
                }
                else
                {
                    return BadRequest(message.Content);
                }
            } catch
            {
                return BadRequest();
            }
        }

        /// <summary>Capture image with specified device</summary>
        /// <param name="gatewayId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the gateway to witch the device is associated with</param>
        /// <param name="deviceId" example="a2d60823-a247-41f5-a89f-9603853c0001">Guid of the device to query</param>
        /// <response code="200">Successful query</response>
        /// <response code="204">Empty query</response>
        /// <response code="400">Invalid query values</response>
        /// <response code="404">Device not found</response>
        [HttpGet]
        [Route("{deviceId:guid}/capture")]
        [ProducesResponseType(typeof(MediaTypeNames.Image), 200)]
        public async Task<ActionResult> GetCapture([FromRoute, BindRequired]Guid? gatewayId = null, [FromRoute, BindRequired]Guid? deviceId = null)
        {
            if (!(deviceId.Value.ToString() == "a2d60823-a247-41f5-a89f-9603853c0001"))
            {
                return NotFound(new Response
                {
                    StatusCode = 404,
                    Error = "Not Found",
                    Message = "Couldn't find device",
                    Description = "Device with Guid " + deviceId.Value.ToString() + " doesn't exist."
                });
            }

            HttpClient client = new HttpClient();
            try
            {
                HttpResponseMessage message = await client.GetAsync("http://home.pedrorendeiro.eu:8080/capture_with_flash");
                if (message.IsSuccessStatusCode)
                {
                    byte[] resultAsBytes = await message.Content.ReadAsByteArrayAsync();
                    return File(resultAsBytes, "image/jpeg");
                }
                else
                {
                    return BadRequest();
                }
            }
            catch
            {
                return NoContent();
            }

        }
        */
    }
}
