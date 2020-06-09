using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;

namespace EMSfIIoT_API.SharedAPI
{
    public class TelegramAPIConnector
    {
        public static TelegramBotClient botClient;

        public TelegramAPIConnector(IConfigurationSection options)
        {
            botClient = new TelegramBotClient(options["TELEGRAM_API_KEY"]);
        }

        public static async Task SendMessageToChat(string chatId, string message)
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: message
            );
        }
    }
}
