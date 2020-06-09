using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Pepipost.Models;
using Pepipost.Exceptions;
using Pepipost.Utilities;
using Pepipost.Http;
using Pepipost.Controllers;

namespace SharedAPI
{
    public class PepiPostAPIConnector
    {
        private static IConfigurationSection _options;

        public PepiPostAPIConnector(IConfigurationSection options)
        {
            _options = options;
        }
        
        public static async Task SendEmail(string to, string subject, string content)
        {
            //initialization of library
            Pepipost.PepipostClient client = new Pepipost.PepipostClient();
            EmailController email = client.Email;
            EmailBody body = new EmailBody();

            string apiKey = _options["PEPIPOST_API_KEY"]; //Add your Pepipost APIkey from panel here

            body.Personalizations = new List<Personalizations>();

            Personalizations body_personalizations_0 = new Personalizations();

            // List of Email Recipients
            body_personalizations_0.Recipient = to; //To/Recipient email address
            body_personalizations_0.Attributes = APIHelper.JsonDeserialize<Object>("{}");
            body.Personalizations.Add(body_personalizations_0);

            body.From = new From();

            // Email Header
            body.From.FromEmail = "api@emsfiiot.pedrorendeiro.eu"; //Sender Email Address. Please note that the sender domain @exampledomain.com should be verified and active under your Pepipost account.
            body.From.FromName = "EMSfIIoT"; //Sender/From name

            //Email Body Content
            body.Subject = subject; //Subject of email
            body.Content = content;
            body.Settings = new Settings
            {
                Footer = 0,
                Clicktrack = 1, //Clicktrack for emails enable=1 | disable=0
                Opentrack = 1, //Opentrack for emails enable=1 | disable=0
                Unsubscribe = 1 //Unsubscribe for emails enable=1 | disable=0
            };
            SendEmailResponse result = await email.CreateSendEmailAsync(apiKey, body);

            try
            {
                if (result.Message.Contains("Error"))
                {
                    Console.WriteLine("\n" + "Message ::" + result.Message + "\n" + "Error Code :: " + result.ErrorInfo.ErrorCode + "\n" + "Error Message ::" + result.ErrorInfo.ErrorMessage + "\n");
                }
                else
                {
                    Console.WriteLine("\n" + "Message ::" + result.Message);
                }

            }
            catch (APIException) { };
        }
    }
}
