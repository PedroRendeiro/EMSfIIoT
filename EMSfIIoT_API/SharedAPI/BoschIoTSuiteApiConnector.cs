using EMSfIIoT_API.Entities;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharedAPI
{
    public class BoschIoTSuiteApiConnector
    {
        private static IConfigurationSection _options;
        private static string _bearerToken;
        private static DateTime _tokenExpiration;
        public static List<Gateway> gateways = new List<Gateway>();

        public BoschIoTSuiteApiConnector(IConfigurationSection options)
        {
            _options = options;

            RefreshBearerToken();
            UpdateGatewaysList();
        }

        private static bool RefreshBearerToken()
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://access.bosch-iot-suite.com" + "/token"),
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {"grant_type", "client_credentials"},
                    {"client_id", _options["client_id"]},
                    {"client_secret", _options["client_secret"]},
                    {"scope", _options["scope"]}
                })
            };
            HttpResponseMessage response = client.SendAsync(request).Result;

            if (response.IsSuccessStatusCode)
            {

                string responseAsString = response.Content.ReadAsStringAsync().Result;

                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };
                jsonSerializerOptions.Converters.Add(new AutoNumberToStringConverter());

                Dictionary<string, string> bearerToken = JsonSerializer.Deserialize<Dictionary<string, string>>(responseAsString, jsonSerializerOptions);

                _bearerToken = bearerToken["access_token"];
                _tokenExpiration = DateTime.Now.AddSeconds(Convert.ToDouble(bearerToken["expires_in"])-60);

                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<Gateway> UpdateGatewaysList()
        {
            if (DateTime.Now > _tokenExpiration)
            {
                RefreshBearerToken();
            }
            
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _options["Server"]+"/search/things");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    gateways = JsonSerializer.Deserialize<Items>(response.Content.ReadAsStringAsync().Result).items;
                    break;
                case HttpStatusCode.Unauthorized:
                    break;
                default:
                    break;
            }

            return gateways;
        }

        public static List<Gateway> GetUserDevices(HttpContext httpContext)
        {
            string userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            return gateways.Where(gateway => gateway.attributes.configuration.users.Contains(userId)).ToList();
        }

        public static bool UpdateDevice(Gateway gateway)
        {
            if (DateTime.Now > _tokenExpiration)
            {
                RefreshBearerToken();
            }

            string thingId = gateway.thingId;

            string body = JsonSerializer.Serialize(gateway);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri(_options["Server"] + $"/things/{thingId}"),
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

           // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Unauthorized:
                    return false;
                default:
                    return false;
            }
        }

        public static bool DeleteDevice(string thingId)
        {
            if (DateTime.Now > _tokenExpiration)
            {
                RefreshBearerToken();
            }

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, _options["Server"] + $"/things/{thingId}");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Unauthorized:
                    return false;
                default:
                    return false;
            }
        }

        public static bool SendMessageToDevice(string thingId, string subject)
        {
            if (DateTime.Now > _tokenExpiration)
            {
                RefreshBearerToken();
            }
            
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(_options["Server"] + $"/things/{thingId}/inbox/messages/{subject}?timeout=10"),
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            Console.WriteLine(response.Content.ReadAsStringAsync().Result);

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Unauthorized:
                    return false;
                default:
                    return false;
            }
        }
    }

    public class AutoNumberToStringConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(string) == typeToConvert;
        }
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.TryGetInt64(out long l) ?
                    l.ToString() :
                    reader.GetDouble().ToString();
            }
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                return document.RootElement.Clone().ToString();
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
