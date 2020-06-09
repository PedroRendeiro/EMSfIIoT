using System;
using System.Net;

namespace EMSfIIoT_API.Models
{
    public class Response
    {
        public int StatusCode { get; set; }

        public string Error { get; set; }

        public string Message { get; set; }

        public string Description { get; set; }
    }
}