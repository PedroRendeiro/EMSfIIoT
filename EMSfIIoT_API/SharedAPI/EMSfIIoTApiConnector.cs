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
using System.Threading.Tasks;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace SharedAPI
{
    public class EMSfIIoTApiConnector
    {
        private static IConfigurationSection _options;

        public EMSfIIoTApiConnector(IConfigurationSection options)
        {
            _options = options;
        }
        
        public static string GetToken(HttpContext httpContext)
        {
            string response;

            try
            {
                // Retrieve the token with the specified scopes
                string[] scope = _options["Scopes"].Split(' ');
                string signedInUserID = httpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                IConfidentialClientApplication cca =
                ConfidentialClientApplicationBuilder.Create(_options["ClientId"])
                    .WithClientSecret(_options["ClientSecret"])
                    .WithB2CAuthority("https://emsfiiot.b2clogin.com/tfp/emsfiiot.onmicrosoft.com/B2C_1_Signin/v2.0")
                    .Build();
                ITokenCache cache = new MSALStaticCache(signedInUserID, httpContext).EnablePersistence(cca.UserTokenCache);

                IEnumerable<IAccount> accounts = cca.GetAccountsAsync().Result;
                AuthenticationResult tokenRequest = cca.AcquireTokenSilent(scope, accounts.FirstOrDefault()).ExecuteAsync().Result;

                if (tokenRequest.AccessToken != null)
                    httpContext.Response.Cookies.Append("ADB2CToken",
                        tokenRequest.AccessToken,
                        new CookieOptions
                        {
                            Expires = tokenRequest.ExpiresOn
                        });

                response = tokenRequest.AccessToken;

            }
            catch (MsalUiRequiredException ex)
            {
                response = $"Session has expired. Please sign in again. {ex.Message}";
                httpContext.Response.Redirect("/Account/SignIn");
            }
            catch (Exception ex)
            {
                response = $"Error calling API: {ex.Message}";
            }

            return response;
        }

        public static void GetNotifications(HttpContext httpContext, out string responseString, out IEnumerable<Notification> notifications)
        {

            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://emsfiiot-api.azurewebsites.net/api/Notifications");

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            notifications = new List<Notification>();

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    responseString = $"API call successful. StatusCode={response.StatusCode}";

                    notifications = JsonSerializer.Deserialize<List<Notification>>(response.Content.ReadAsStringAsync().Result,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    notifications = notifications.OrderByDescending(x => x.CreatedAt);
                    break;
                case HttpStatusCode.Unauthorized:
                    responseString = $"Please sign in again. {response.ReasonPhrase}";
                    httpContext.Response.Redirect("/Account/SignIn");
                    break;
                default:
                    responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                    break;
            }
        }

        public async static Task<bool> MarkNotificationsAsRead(HttpContext httpContext)
        {
            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Patch, $"https://emsfiiot-api.azurewebsites.net/api/Notifications");

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    return true;
                case HttpStatusCode.Unauthorized:
                    httpContext.Response.Redirect("/Account/SignIn");
                    return false;
                default:
                    return false;
            }
        }

        public static void GetUnReadNotifications(HttpContext httpContext, out string responseString, out IEnumerable<Notification> notifications)
        {

            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);            

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://emsfiiot-api.azurewebsites.net/api/Notifications?filter=unread");

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            notifications = new List<Notification>();

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    responseString = $"API call successful. StatusCode={response.StatusCode}";

                    notifications = JsonSerializer.Deserialize<List<Notification>>(response.Content.ReadAsStringAsync().Result,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        });
                    notifications = notifications.OrderByDescending(x => x.CreatedAt);
                    break;
                case HttpStatusCode.Unauthorized:
                    responseString = $"Please sign in again. {response.ReasonPhrase}";
                    httpContext.Response.Redirect("/Account/SignIn");
                    break;
                default:
                    responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                    break;
            }
        }

        public static void GetEvents(HttpContext httpContext, out string responseString, out IEnumerable<Event> events)
        {

            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"https://emsfiiot-api.azurewebsites.net/api/Events");

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = client.SendAsync(request).Result;

            events = new List<Event>();

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    responseString = $"API call successful. StatusCode={response.StatusCode}";

                    JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

                    events = JsonSerializer.Deserialize<List<Event>>(response.Content.ReadAsStringAsync().Result, jsonSerializerOptions);
                    events = events.OrderByDescending(x => x.Id);
                    break;
                case HttpStatusCode.Unauthorized:
                    responseString = $"Please sign in again. {response.ReasonPhrase}";
                    httpContext.Response.Redirect("/Account/SignIn");
                    break;
                default:
                    responseString = $"Error calling API. StatusCode=${response.StatusCode}";
                    break;
            }
        }

        public async static Task<bool> DeleteEvent(HttpContext httpContext, long eventId)
        {
            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, $"https://emsfiiot-api.azurewebsites.net/api/Events/{eventId}");

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return true;
                case HttpStatusCode.Unauthorized:
                    httpContext.Response.Redirect("/Account/SignIn");
                    return false;
                default:
                    return false;
            }
        }

        public async static Task<bool> UpdateEvent(HttpContext httpContext, long eventId, EventDTO @event)
        {

            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            string body = JsonSerializer.Serialize(@event);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://emsfiiot-api.azurewebsites.net/api/Events/{eventId}"),
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    return true;
                case HttpStatusCode.Unauthorized:
                    httpContext.Response.Redirect("/Account/SignIn");
                    return false;
                default:
                    return false;
            }
        }

        public async static Task<bool> CreateEvent(HttpContext httpContext, EventDTO @event)
        {

            httpContext.Request.Cookies.TryGetValue("ADB2CToken", out string AccessToken);

            if (AccessToken == null)
                AccessToken = GetToken(httpContext);

            string body = JsonSerializer.Serialize(@event);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://emsfiiot-api.azurewebsites.net/api/Events"),
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };

            // Add token to the Authorization header and make the request
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            // Handle the response
            switch (response.StatusCode)
            {
                case HttpStatusCode.NoContent:
                    return true;
                case HttpStatusCode.Unauthorized:
                    httpContext.Response.Redirect("/Account/SignIn");
                    return false;
                default:
                    return false;
            }
        }
    }
}