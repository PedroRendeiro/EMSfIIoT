using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace SharedAPI
{
    public class MeasuresApiConnector
    {        
        public static void GetMeasures(HttpContext httpContext, out IEnumerable<Measure> measures)
        {
            List<Measure> values = new List<Measure>();

            string userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            string dateTime = DateTime.UtcNow.AddDays(-14).ToString("yyyy-MM-ddTHH:mm:ss");
            BoschIoTSuiteApiConnector.gateways.Where(gateway => gateway.attributes.configuration.users.Contains(userId)).ToList().ForEach(gateway =>
            {
                gateway.attributes.configuration.devices.ForEach(device =>
                {
                    HttpClient client = new HttpClient();
                    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://measures-api.azurewebsites.net/api/Measures?datetime={dateTime}&locationId={device.locationId}");
                    HttpResponseMessage response = client.SendAsync(request).Result;

                    // Handle the response
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            var result = JsonSerializer.Deserialize<List<Measure>>(response.Content.ReadAsStringAsync().Result,
                                new JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                });
                            values.AddRange(result.OrderBy(x => x.TimeStamp));
                            break;
                        default:
                            break;
                    }
                });
            });

            measures = values;
        }

        public static void GetDeviceLastMeasures(long locationId, out IEnumerable<Measure> measures, int skip = 0)
        {
            List<Measure> values = new List<Measure>();

            for (int measureTypeID = 1; measureTypeID <= 3; measureTypeID++)
            {
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://measures-api.azurewebsites.net/api/Measures?locationId={locationId}&measureTypeID={measureTypeID}&limit=1&skip={skip}");
                HttpResponseMessage response = client.SendAsync(request).Result;

                // Handle the response
                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var result = JsonSerializer.Deserialize<List<Measure>>(response.Content.ReadAsStringAsync().Result,
                            new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            });
                        values.AddRange(result.OrderBy(x => x.TimeStamp));
                        break;
                    default:
                        break;
                }
            }

            measures = values;
        }

        public static void GetDeviceMeasures(long locationId, out IEnumerable<Measure> measures, int timeSpanMinutes, int skip = 0)
        {
            string dateTime = DateTime.UtcNow.AddMinutes(-timeSpanMinutes).ToString("yyyy/MM/ddZHH:mm:ss");

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://measures-api.azurewebsites.net/api/Measures?locationId={locationId}&dateTime={dateTime}&skip={skip}");
            HttpResponseMessage response = client.SendAsync(request).Result;

            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    var result = JsonSerializer.Deserialize<List<Measure>>(response.Content.ReadAsStringAsync().Result,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    measures = result.OrderBy(x => x.TimeStamp);
                    break;
                default:
                    measures = new List<Measure>();
                    break;
            }
        }
    }
}