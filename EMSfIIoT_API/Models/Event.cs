using EMSfIIoT_API.Helpers;
using SharedAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;

namespace EMSfIIoT_API.Models
{
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        [Required]
        public Guid Username { get; set; }
        [Required]
        public string Variable { get; set; }
        [Required]
        public EventType EventType { get; set; }
        [Required]
        public EventValueType EventValueType { get; set; }
        [Required]
        public long EventValue { get; set; }
        [Required]
        public EventFrequency EventFrequency { get; set; }
        [Required]
        public NotificationType NotificationType { get; set; }
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime LastUpdate { get; set; }
        public bool OnHold { get; set; }
    }

    public class EventDTO : IValidatableObject
    {
        /// <summary>
        /// DeviceId to track
        /// </summary>
        /// <example>Gateway1:ESP32_CAM1</example>
        [Required]
        public string Variable { get; set; }

        [Required]
        public EventType EventType { get; set; }

        [Required]
        public EventValueType EventValueType { get; set; }

        [Required]
        public long EventValue { get; set; }

        [Required]
        public EventFrequency EventFrequency { get; set; }

        [Required]
        public NotificationType NotificationType { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (EventType.Equals(EventType.Inactive))
            {
                if (EventValueType != EventValueType.Empty)
                {
                    results.Add(new ValidationResult($"The EventValueType must be Empty for EventType.{EventType}", new string[] { "EventValueType" }));
                }

                if (EventFrequency != EventFrequency.EveryMinute)
                {
                    results.Add(new ValidationResult($"The EventFrequency must be EveryMinute for EventType.{EventType}", new string[] { "EventFrequency" }));
                }

                if (!BoschIoTSuiteApiConnector.gateways.Select(gateway => gateway.thingId).ToList().Contains(Variable))
                {
                    results.Add(new ValidationResult($"The Variable must be a valid Bosch IoT Things ThingId", new string[] { "Variable" }));
                }
            }

            if (EventType.Equals(EventType.Threshold))
            {
                if (EventValueType.Equals(EventValueType.Empty))
                {
                    results.Add(new ValidationResult($"The EventValueType must not be {EventValueType} for EventType.{EventType}", new string[] { "EventValueType" }));
                }

                if (EventFrequency != EventFrequency.Never)
                {
                    results.Add(new ValidationResult($"The EventFrequency must be Never for EventType.{EventType}", new string[] { "EventFrequency" }));
                }

                if (!BoschIoTSuiteApiConnector.gateways.Select(gateway => gateway.attributes.configuration.devices).Select(devices => devices.Where(device => device.deviceId.Equals(Variable))).First().Any())
                {
                    results.Add(new ValidationResult($"The Variable must be a valid Bosch IoT Things device", new string[] { "Variable" }));
                }
            }

            if (EventType.Equals(EventType.Algorithm))
            {
                if (EventValueType != EventValueType.Empty)
                {
                    results.Add(new ValidationResult($"The EventValueType must be Empty for EventType.{EventType}", new string[] { "EventValueType" }));
                }

                if (EventFrequency == EventFrequency.Never | EventFrequency == EventFrequency.EveryMinute)
                {
                    results.Add(new ValidationResult($"The EventFrequency must not be {EventFrequency} for EventType.{EventType}", new string[] { "EventFrequency" }));
                }

                if (!BoschIoTSuiteApiConnector.gateways.Select(gateway => gateway.attributes.configuration.devices).Select(devices => devices.Where(device => device.deviceId.Equals(Variable))).First().Any())
                {
                    results.Add(new ValidationResult($"The Variable must be a valid Bosch IoT Things device", new string[] { "Variable" }));
                }
            }

            return results;
        }
    }

    [Flags]
    public enum NotificationType
    {
        None = 0,
        Email = 1 << 0,
        PopUp = 1 << 1,
        Telegram = 1 << 2
    }

    public enum EventType
    {
        Threshold,
        Inactive,
        Algorithm
    }

    public enum EventValueType
    {
        Empty,
        EqualTo,
        NotEqualTo,
        GreaterThan,
        LessThan,
        GreaterThanOrEqualTo,
        LessThanOrEqualTo
    }

    public enum EventFrequency
    {
        [StringValue("Never"), CronValue("")]
        Never,
        [StringValue("Every minute"), CronValue("0 * * * * *")]
        EveryMinute,
        [StringValue("Every 15 minutes"), CronValue("0 */15 * * * *")]
        Every15Minutes,
        [StringValue("Every 30 minutes"), CronValue("0 */30 * * * *")]
        Every30Minutes,
        [StringValue("Every hour"), CronValue("0 0 */1 * * *")]
        EveryHour,
        [StringValue("Every 6 hours"), CronValue("0 0 */6 * * *")]
        Every6Hours,
        [StringValue("Every day"), CronValue("0 0 0 * * *")]
        EveryDay,
        [StringValue("Every week"), CronValue("0 0 0 * * 1")]
        EveryWeek
    }
}
