using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

using EMSfIIoT_API.Models;
using System.Text.Json;
using System.Threading;
using SharedAPI;

namespace EMSfIIoT.Pages.Admin
{
    public class GatewaysModel : PageModel
    {
        public bool isAdmin = false;
        public static List<Gateway> gateways;

        public void OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                isAdmin = GraphApiConnector.administrators.Contains(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (!isAdmin)
                {
                    HttpContext.Response.Redirect("/");
                }

                gateways = BoschIoTSuiteApiConnector.gateways;

                ViewData["gateways"] = JsonSerializer.Serialize(gateways).Replace("\"", "\\\"");

            }
        }

        public void OnPost()
        {
            var form = Request.Form;

            if (form.TryGetValue("gatewayDeleteId", out var value))
            {
                BoschIoTSuiteApiConnector.DeleteDevice(value);
            }
            else
            {
                string thingId = form["thingId"];
                Gateway gateway = gateways.Where(gateway => gateway.thingId.Equals(thingId)).FirstOrDefault();

                if (gateway == null)
                {
                    gateway = new Gateway();
                    gateway.attributes = new GatewayAttributes();
                    gateway.attributes.configuration = new GatewayConfiguration();
                    
                    gateway.features = new GatewayFeatures();
                    gateway.features.ESP32_CAM = new GatewayESP32_CAM();
                    gateway.features.ESP32_CAM.definition = new List<string>();
                    gateway.features.ESP32_CAM.definition.Add("EMSfIIoT:ESP32_CAM:1.0.0");
                    gateway.features.ESP32_CAM.properties = new ESP32_CAMProperties();
                    gateway.features.ESP32_CAM.properties.status = new Measure();
                    
                    gateway.thingId = thingId;
                    gateway.policyId = form["policyId"];
                    gateway.definition = form["definition"];
                }

                gateway.attributes.thingName = form["thingName"];
                gateway.attributes.manufacturer = form["manufacturer"];
                gateway.attributes.model = form["model"];
                gateway.attributes.serialNo = form["serialNo"];
                gateway.attributes.location = form["location"];

                gateway.attributes.configuration.devices = new List<Devices>();
                gateway.attributes.configuration.users = new List<string>();
                for (int i = 0; i < 255; i++)
                {
                    if (Request.Form.TryGetValue("deviceId" + i.ToString(), out var deviceId) &
                        Request.Form.TryGetValue("url" + i.ToString(), out var url) &
                        Request.Form.TryGetValue("locationId" + i.ToString(), out var locationId) &
                        deviceId != "" & url != "" & locationId != "")
                    {
                        gateway.attributes.configuration.devices.Add(new Devices
                        {
                            deviceId = deviceId,
                            url = new Uri(url),
                            locationId = Convert.ToInt64(locationId)
                        });
                    }

                    if (Request.Form.TryGetValue("userId" + i.ToString(), out var userId) &
                        userId != "")
                    {
                        gateway.attributes.configuration.users.Add(userId);
                    }
                }

                BoschIoTSuiteApiConnector.UpdateDevice(gateway);
                Thread.Sleep(1000);
                BoschIoTSuiteApiConnector.UpdateGatewaysList();
            }

            OnGet();
        }
    }
}