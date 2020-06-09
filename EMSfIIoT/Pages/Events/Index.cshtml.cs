using EMSfIIoT_API.Helpers;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SharedAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace EMSfIIoT.Pages.Events
{
    public class IndexModel : PageModel
    {
        public IEnumerable<Event> events;

        public void OnGet()
        {
            EMSfIIoTApiConnector.GetEvents(HttpContext, out _, out events);

            JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
            jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            ViewData["events"] = JsonSerializer.Serialize(events.ToList(), jsonSerializerOptions).Replace("\"", "\\\"");

            List<string> devices = new List<string>();
            List<string> gateways = new List<string>();
            foreach (Gateway gateway in BoschIoTSuiteApiConnector.GetUserDevices(HttpContext))
            {
                gateways.Add(gateway.thingId);
                foreach (Devices device in gateway.attributes.configuration.devices)
                {
                    devices.Add(device.deviceId);
                }
            }
            ViewData["Variable"] = JsonSerializer
                .Serialize(devices)
                .Replace("\"", "\\\"");
            ViewData["Gateways"] = JsonSerializer
                .Serialize(gateways)
                .Replace("\"", "\\\"");

            List<string> notificationType = Enum.GetNames(typeof(NotificationType)).ToList();
            notificationType.Remove(NotificationType.None.ToString());
            ViewData["NotificationType"] = JsonSerializer
                .Serialize(notificationType,
                    jsonSerializerOptions)
                .Replace("\"", "\\\"");
            
            ViewData["EventType"] = JsonSerializer
                .Serialize(
                    Enum.GetNames(typeof(EventType)).ToList(),
                    jsonSerializerOptions)
                .Replace("\"", "\\\"");
            
            ViewData["EventValueType"] = JsonSerializer
                .Serialize(
                    Enum.GetNames(typeof(EventValueType)).ToList(),
                    jsonSerializerOptions)
                .Replace("\"", "\\\"");

            List<string> eventFrequencyStringValues = typeof(EventFrequency).GetStringValues().ToList();
            List<string> eventFrequencyEnumValues = typeof(EventFrequency).GetEnumNames().ToList();
            //eventFrequencyEnumValues.Remove("Never");
            Dictionary<string, string> eventFrequency = eventFrequencyEnumValues
                .Zip(eventFrequencyStringValues, (enumValue, stringValue) => new { enumValue, stringValue })
                .ToDictionary(eventFrequency => eventFrequency.enumValue, eventFrequency => eventFrequency.stringValue);
            ViewData["EventFrequency"] = JsonSerializer
                .Serialize(eventFrequency,
                    jsonSerializerOptions)
                .Replace("\"", "\\\"");
        }

        public async Task OnPost()
        {
            var form = Request.Form;

            if (form.TryGetValue("eventDeleteId", out var value))
            {
                await EMSfIIoTApiConnector.DeleteEvent(HttpContext, Convert.ToInt64(value));
            }
            else
            {                
                EventDTO @event = new EventDTO()
                {
                    Variable = form["Variable"],
                    EventType = (EventType)Enum.Parse(typeof(EventType), form["EventType"]),
                    EventValueType = (EventValueType)Enum.Parse(typeof(EventValueType), form["EventValueType"]),
                    EventValue = Convert.ToInt64(form["EventValue"]),
                    EventFrequency = (EventFrequency)Enum.Parse(typeof(EventFrequency), form["EventFrequency"]),
                    NotificationType = (form.ContainsKey("Email") ? NotificationType.Email : NotificationType.None) |
                        (form.ContainsKey("PopUp") ? NotificationType.PopUp : NotificationType.None) |
                        (form.ContainsKey("Telegram") ? NotificationType.Telegram : NotificationType.None)
                };

                form.TryGetValue("eventId", out var eventId);

                if (eventId != "")
                {
                    await EMSfIIoTApiConnector.UpdateEvent(HttpContext, Convert.ToInt64(form["eventId"]), @event);
                }
                else
                {
                    await EMSfIIoTApiConnector.CreateEvent(HttpContext, @event);
                }
            }

            OnGet();
        }
    }
}