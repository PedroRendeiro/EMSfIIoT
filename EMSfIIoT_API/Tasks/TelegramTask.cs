using System;
using System.Threading;
using System.Threading.Tasks;
using SharedAPI;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Args;
using Microsoft.Extensions.Hosting;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Generic;
using SendGrid.Helpers.Mail;
using EMSfIIoT_API.SharedAPI;

namespace EMSfIIoT_API.Tasks
{
    public class TelegramTask : BackgroundService
    {
        private static TelegramBotClient botClient;
        private static List<TelgramSubscription> subscriptions = new List<TelgramSubscription>();

        public TelegramTask()
        {
            botClient = TelegramAPIConnector.botClient;
            botClient.OnMessage += Bot_OnMessage;
            botClient.StartReceiving();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                await Task.Delay(100, stoppingToken);
                subscriptions.ToList().ForEach(subscription =>
                {
                    if (DateTime.Now > subscription.MessageDate.AddHours(1))
                    {
                        subscriptions.Remove(subscription);
                    }
                });
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        private static async void Bot_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Message.Text != null)
            {
                await GraphApiConnector.UpdateUsers();
                BotCommand[] commands = await botClient.GetMyCommandsAsync();

                await botClient.SendChatActionAsync(e.Message.Chat.Id, ChatAction.Typing);
                string response = null;

                if (e.Message.ReplyToMessage != null)
                {
                    var subscription = subscriptions.Where(subscription => subscription.MessageId.Equals(e.Message.ReplyToMessage.MessageId)).FirstOrDefault();

                    if (subscription != null)
                    {
                        if (subscription.WaitingEmail)
                        {
                            if (e.Message.Text.Trim().Equals(subscription.Token))
                            {
                                subscriptions.Remove(subscription);
                                await GraphApiConnector.AddTelegramSubscription(subscription.User, e.Message.Chat.Id.ToString());
                                response = "This chat is now associated with account with email address " + subscription.User.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).First().IssuerAssignedId;
                            }
                            else
                            {
                                response = "Wrong code";
                            }
                        }
                        else
                        {
                            var user = GraphApiConnector.users
                                .Where(user => user.Identities
                                    .Where(identity => identity.SignInType.Equals("emailAddress")).First()
                                        .IssuerAssignedId.Equals(e.Message.Text.ToString().Trim().ToLower()))
                                .FirstOrDefault();

                            if (user != null)
                            {
                                if (!user.AdditionalData.TryGetValue("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId", out _))
                                {
                                    Random r = new Random();
                                    string randNum = r.Next(1000000).ToString("D6");

                                    subscription.User = user;
                                    subscription.Token = randNum;
                                    subscription.WaitingEmail = true;

                                    await SendGridAPIConnector.SendEmail(new EmailAddress()
                                    {
                                        Email = user.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).First().IssuerAssignedId,
                                        Name = user.DisplayName
                                    },
                                    "EMSfIIoT TelgramBot",
                                    randNum,
                                    randNum);

                                    response = "Please insert the code you received in your email address";
                                    var message = await botClient.SendTextMessageAsync(
                                        chatId: e.Message.Chat,
                                        text: response,
                                        replyMarkup: new ForceReplyMarkup()
                                    );
                                    subscription.MessageId = message.MessageId;
                                    subscription.MessageDate = DateTime.Now;
                                    return;
                                }
                                else
                                {
                                    response = "This account is already associated with another chat";
                                }
                            }
                            else
                            {
                                response = "User with email address " + e.Message.Text.ToString().Trim().ToLower() + " not found";
                            }
                        }
                    }
                    else
                    {
                        response = "Invalid request. Please try again.";
                    }

                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: response
                    );
                    return;
                }

                switch (e.Message.Text)
                {
                    case "/start":
                        response = "Hello " + e.Message.From.FirstName + " " + e.Message.From.LastName + "\n";
                        response += "Welcome to the EMSfIIoT Telegram Bot!\n";
                        commands.ToList().ForEach(command =>
                        {
                            response += "\n/" + command.Command + " - " + command.Description;
                        });
                        break;
                    case "/help":
                        commands.ToList().ForEach(command =>
                        {
                            response += "/" + command.Command + " - " + command.Description + "\n";
                        });
                        break;
                    case "/subscribe":
                        foreach (var user in GraphApiConnector.users)
                        {
                            if (user.AdditionalData.TryGetValue("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId", out var value))
                            {
                                if (Convert.ToInt64(value).Equals(e.Message.Chat.Id))
                                {
                                    response = "This chat is already associated with an account";
                                }
                            }
                        }
                        if (response != null)
                        {
                            break;
                        }

                        response = "Please insert your EMSfIIoT account email address";
                        var message = await botClient.SendTextMessageAsync(
                            chatId: e.Message.Chat,
                            text: response,
                            replyMarkup: new ForceReplyMarkup()
                        );
                        subscriptions.Add(new TelgramSubscription()
                        {
                            MessageId = message.MessageId,
                            MessageDate = DateTime.Now,
                            WaitingEmail = false
                        });
                        return;
                    case "/unsubscribe":
                        foreach (var user in GraphApiConnector.users)
                        {
                            if (user.AdditionalData.TryGetValue("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId", out var value))
                            {
                                if (Convert.ToInt64(value).Equals(e.Message.Chat.Id))
                                {
                                    response = user.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).First().IssuerAssignedId;

                                    await GraphApiConnector.RemoveTelegramSubscription(user);
                                }
                            }
                        }
                        if (response != null)
                        {
                            response = "Your subscription associated with account with email address " + response + " was removed";
                        }
                        else
                        {
                            response = "This chat isn't associated with any account";
                        }
                        break;
                    case "/status":
                        foreach (var user in GraphApiConnector.users)
                        {
                            if (user.AdditionalData.TryGetValue("extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId", out var value))
                            {
                                if (Convert.ToInt64(value).Equals(e.Message.Chat.Id))
                                {
                                    response = user.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).First().IssuerAssignedId;
                                }
                            }
                        }
                        if (response != null)
                        {
                            response = "This chat is associated with account with email address " + response;
                        }
                        else
                        {
                            response = "This chat isn't associated with any account";
                        }
                        break;
                    default:
                        response = "Command not recognized! Type /help for a list of commands.";
                        break;
                }

                if (response != null)
                {
                    await botClient.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: response
                    );
                }
            }
        }
    }

    public class TelgramSubscription
    {
        public int MessageId { get; set; }
        public DateTime MessageDate { get; set; }
        public bool WaitingEmail { get; set; }
        public Microsoft.Graph.User User { get; set; }
        public string Token { get; set; }
    }
}
