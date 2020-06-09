using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace EMSfIIoT_API.Models
{
    public class Items
    {
        public List<Gateway> items { get; set; }
    }

    public class Gateway
    {
        [Key]
        public string thingId { get; set; }

        public string policyId { get; set; }

        public string definition { get; set; }

        public GatewayAttributes attributes { get; set; }
        public GatewayFeatures features { get; set; }
    }

    public class GatewayAttributes
    {
        public string thingName { get; set; }

        public string manufacturer { get; set; }

        public string model { get; set; }

        public string serialNo { get; set; }

        public string location { get; set; }

        public GatewayConfiguration configuration { get; set; }
    }

    public class GatewayConfiguration
    {
        public List<Devices> devices { get; set; }
        public List<string> users { get; set; }
        public long cadence { get; set; }
    }

    public class Devices
    {
        public string deviceId { get; set; }
        public Uri url { get; set; }
        public long locationId { get; set; }
    }

    public class GatewayFeatures
    {
        public GatewayESP32_CAM ESP32_CAM { get; set; }
    }

    public class GatewayESP32_CAM
    {
        public List<string> definition { get; set; }

        public ESP32_CAMProperties properties { get; set; }
    }

    public class ESP32_CAMProperties
    {
        public Measure status { get; set; }
    }
}