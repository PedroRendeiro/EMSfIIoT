using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SharedAPI;

namespace EMSfIIoT.Pages
{
    public class UserModel : PageModel
    {
        public async Task OnGet()
        {
            await GraphApiConnector.UpdateUsers();
        }
    }
}