using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharedAPI
{
    public class SendGridAPIConnector
    {
        private static IConfigurationSection _options;

        public SendGridAPIConnector(IConfigurationSection options)
        {
            _options = options;
        }
        
        public static async Task SendEmail(EmailAddress to, string subject, string plainTextContent, string htmlContent)
        {
            var apiKey = _options["SENDGRID_API_KEY"];
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("api@emsfiiot.pedrorendeiro.eu", "EMSfIIoT");
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }
    }
}
