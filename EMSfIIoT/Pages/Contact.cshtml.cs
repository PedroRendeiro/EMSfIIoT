using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.V3.Pages.Account.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace EMSfIIoT.Pages
{

    [BindProperties]
    public class ContactModel : PageModel
    {
        [Display(Name = "Message"), Required(ErrorMessage = "Message Required")]
        public string Message { get; set; }
        [Display(Name = "First Name"), Required(ErrorMessage = "First Name Required")]
        public string FirstName { get; set; }
        [Display(Name = "Last Name"), Required(ErrorMessage = "Last Name Required")]
        public string LastName { get; set; }
        [Display(Name = "Email"), Required(ErrorMessage = "Email Required"), DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public ContactModel(ILogger<RegisterModel> logger, IConfiguration configuration)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public void OnGet()
        {

        }

        public async Task<IActionResult> OnPostAsync()
        {
            string recaptchaResponse = Request.Form["g-recaptcha-response"];
            HttpClient client = new HttpClient();
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    {"secret", configuration["reCAPTCHA:SecretKey"]},
                    {"response", recaptchaResponse},
                    {"remoteip", HttpContext.Connection.RemoteIpAddress.ToString()}
                };

                HttpResponseMessage response = await client.PostAsync("https://www.google.com/recaptcha/api/siteverify", new FormUrlEncodedContent(parameters));
                response.EnsureSuccessStatusCode();

                string apiResponse = await response.Content.ReadAsStringAsync();
                dynamic apiJson = JObject.Parse(apiResponse);
                if (apiJson.success != true)
                {
                    this.ModelState.AddModelError(string.Empty, "There was an unexpected problem processing this request. Please try again.");
                }
            }
            catch (HttpRequestException ex)
            {
                // Something went wrong with the API. Let the request through.
                logger.LogError(ex, "Unexpected error calling reCAPTCHA api.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // ... continue with registration ...
            foreach (var form in Request.Form)
            {
                Console.WriteLine(form.Key + ": " + form.Value);
            }

            return Page();
        }
    }
}