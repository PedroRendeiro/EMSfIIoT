using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using EMSfIIoT_API.Models;
using SharedAPI;

namespace EMSfIIoT.Pages.Events
{    
    public class NotificationsModel : PageModel
    {
        public IEnumerable<Notification> notifications;
        public async Task OnGet()
        {
            await EMSfIIoTApiConnector.MarkNotificationsAsRead(HttpContext);
            
            EMSfIIoTApiConnector.GetNotifications(HttpContext, out _, out notifications);
        }
    }
}