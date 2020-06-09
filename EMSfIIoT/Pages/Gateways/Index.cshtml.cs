using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EMSfIIoT_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SharedAPI;

namespace EMSfIIoT.Pages.Gateways
{   
    public class IndexModel : PageModel
    {
        public List<Gateway> gateways;

        public void OnGet()
        {
            gateways = BoschIoTSuiteApiConnector.GetUserDevices(HttpContext);
        }

        public void OnPost()
        {
            var form = Request.Form;

            if (form.TryGetValue("gatewayId", out _) & form.TryGetValue("operation", out _))
            {
                BoschIoTSuiteApiConnector.SendMessageToDevice(form["gatewayId"], form["operation"].ToString().ToLower());
            }

            OnGet();
        }
    }
}