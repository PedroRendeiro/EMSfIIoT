using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace EMSfIIoT.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        public string RequestId { get; set; }
        public string Message { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        private readonly ILogger<ErrorModel> _logger;

        public ErrorModel(ILogger<ErrorModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            if (HttpContext.Request.Query.TryGetValue("message", out var value))
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
                if (value.ToString().Contains("AADB2C99002"))
                {
                    Message = "Sign in failed because your account was not found.";
                }
            }
            else
            {
                HttpContext.Response.Redirect("/");
            }
        }
    }
}