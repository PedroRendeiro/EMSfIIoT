using System;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Options;
using EMSfIIoT_API.Models;
using Microsoft.Extensions.Configuration;

namespace SharedAPI
{
    public class GraphApiConnector
    {
        private static IConfigurationSection _options;
        private static GraphServiceClient graphClient;
        public static List<string> administrators = new List<string>();
        public static List<User> users = new List<User>();

        public GraphApiConnector(IConfigurationSection options)
        {
            _options = options;

            // Build a client application.
            IConfidentialClientApplication confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(_options["ClientId"])
                .WithTenantId(_options["TenantId"])
                .WithClientSecret(_options["ClientSecret"])
                .Build();
            // Create an authentication provider by passing in a client application and graph scopes.
            ClientCredentialProvider authProvider = new ClientCredentialProvider(confidentialClientApplication);

            // Create a new instance of GraphServiceClient with the authentication provider.
            graphClient = new GraphServiceClient(authProvider);

            UpdateUsers().Wait();
        }

        public static async Task<List<string>> UpdateAdministrators()
        {
            // GET https://graph.microsoft.com/beta/users
            var request = await graphClient.Groups[_options["AdminGroup"]]
                .Members
                .Request()
                .GetAsync();

            administrators.Clear();

            foreach (var admin in request)
            {
                administrators.Add(admin.Id);
            }

            return administrators;
        }

        public static async Task<List<User>> UpdateUsers()
        {
            // GET https://graph.microsoft.com/beta/users
            var request = await graphClient.Users
                .Request()
                .GetAsync();

            users = request.Where(user => user.Identities.Select(identity => identity.SignInType).Contains("emailAddress")).ToList();

            return users;
        }

        public static async Task<User> CreateUser(User user)
        {
            // GET https://graph.microsoft.com/beta/users
            var request = await graphClient.Users
                .Request()
                .AddAsync(user);

            await UpdateUsers();

            return request;
        }

        public static async Task<User> UpdateUser(User user)
        {
            // GET https://graph.microsoft.com/beta/users
            var request = await graphClient.Users[user.Id]
                .Request()
                .UpdateAsync(user);

            await UpdateUsers();

            return request;
        }

        public static async Task<bool> DeleteUser(string Id)
        {
            // GET https://graph.microsoft.com/beta/users
            await graphClient.Users[Id]
                .Request()
                .DeleteAsync();

            await UpdateUsers();

            return true;
        }

        public static async Task<User> RemoveAdministrator(User user)
        {
            await graphClient.Groups[_options["AdminGroup"]]
                .Members[user.Id]
                .Reference
                .Request()
                .DeleteAsync();

            await UpdateAdministrators();

            return user;
        }

        public static async Task<User> AddAdministrator(User user)
        {
            // GET https://graph.microsoft.com/beta/users
            await graphClient.Groups[_options["AdminGroup"]]
                .Members
                .References
                .Request()
                .AddAsync(user);

            await UpdateAdministrators();

            return user;
        }

        public static async Task<bool> AddTelegramSubscription(User user, string telegramChatId)
        {
            if (user.AdditionalData.ContainsKey("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId"))
            {
                user.AdditionalData["extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId"] = telegramChatId;
            }
            else
            {
                user.AdditionalData.Add("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId", telegramChatId);
            }           

            await UpdateUser(user);

            return true;
        }

        public static async Task<bool> RemoveTelegramSubscription(User user)
        {
            user.AdditionalData["extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId"] = "";

            await UpdateUser(user);

            return true;
        }
    }
}