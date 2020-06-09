using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EMSfIIoT_API.Entities;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using SharedAPI;

namespace EMSfIIoT.Pages.Admin
{
    public class UsersModel : PageModel
    {
        public static List<AppUser> users = new List<AppUser>();
        public static List<string> administrators;
        public async Task OnGet()
        {
            var graphUsers = await GraphApiConnector.UpdateUsers();
            administrators = await GraphApiConnector.UpdateAdministrators();

            users.Clear();

            graphUsers.ToList().ForEach(user =>
            {
                users.Add(new AppUser
                {
                    Username = user.Id,
                    DisplayName = user.DisplayName,
                    EmailAddress = user.Identities.First(identity => identity.SignInType.Equals("emailAddress")).IssuerAssignedId.ToString()
                });
            });

            ViewData["users"] = JsonSerializer.Serialize(users.ToList()).Replace("\"", "\\\"");
            ViewData["administrators"] = JsonSerializer.Serialize(administrators.ToList()).Replace("\"", "\\\"");
        }

        public async Task OnPost()
        {
            var form = Request.Form;

            if (form.TryGetValue("userDeleteId", out var value))
            {
                await GraphApiConnector.DeleteUser(value);
            }
            else
            {
                Microsoft.Graph.User user;

                var list = new List<ObjectIdentity>();

                if (users.Where(a => a.Username.Equals(form["userId"])).Any())
                {
                    list = GraphApiConnector.users.Where(a => a.Id.Equals(form["userId"])).First().Identities.ToList();
                    list.Where(identity => identity.SignInType.Equals("emailAddress")).First().IssuerAssignedId = form["EmailAddress"];

                    user = new Microsoft.Graph.User
                    {
                        Id = form["userId"],
                        AccountEnabled = true,
                        DisplayName = form["DisplayName"],
                        CreationType = "LocalAccount",
                        Identities = list
                    };
                    await GraphApiConnector.UpdateUser(user);
                }
                else
                {
                    list.Add(new ObjectIdentity
                    {
                        SignInType = "emailAddress",
                        Issuer = "emsfiiot.onmicrosoft.com",
                        IssuerAssignedId = form["EmailAddress"]
                    });

                    user = new Microsoft.Graph.User
                    {
                        Id = form["userId"],
                        AccountEnabled = true,
                        DisplayName = form["DisplayName"],
                        CreationType = "LocalAccount",
                        PasswordProfile = new PasswordProfile
                        {
                            ForceChangePasswordNextSignInWithMfa = true,
                            Password = "Emk\9M6Qy/;%4?>]"
                        },
                        Identities = list
                    };
                    await GraphApiConnector.CreateUser(user);
                }

                if (!administrators.Contains(user.Id) & form["Administrator"].ToString().Length > 0)
                {
                    await GraphApiConnector.AddAdministrator(user);
                }
                else if (administrators.Contains(user.Id) & !(form["Administrator"].ToString().Length > 0))
                {
                    await GraphApiConnector.RemoveAdministrator(user);
                }

                Thread.Sleep(1000);
            }

            await OnGet();
        }
    }
}