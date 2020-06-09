using EMSfIIoT_API.Controllers;
using EMSfIIoT_API.DbContexts;
using EMSfIIoT_API.Helpers;
using EMSfIIoT_API.Hubs;
using EMSfIIoT_API.Models;
using EMSfIIoT_API.SharedAPI;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using NCrontab;
using SharedAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Event = EMSfIIoT_API.Models.Event;

namespace EMSfIIoT_API.Tasks
{
    public class NotificationTask : BackgroundService
    {
        private static ApiDbContext _context;
        private static NotificationsController _notifications;
        private static EventsController _events;
        private static IHubContext<NotificationsHub> _notificationsHub;

        private static Dictionary<long, CrontabSchedule> crontabSchedule = new Dictionary<long, CrontabSchedule>();
        private static Dictionary<long, DateTime> crontabNextRun = new Dictionary<long, DateTime>();

        public NotificationTask(IHubContext<NotificationsHub> notificationsHub, IServiceScopeFactory serviceScopeFactory)
        {
            _context = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApiDbContext>();
            _notifications = new NotificationsController(_context);
            _events = new EventsController(_context);
            _notificationsHub = notificationsHub;

            _context.Event.AsNoTracking().ForEachAsync(@event =>
            {
                AddTaskToSchedule(@event);
            }).Wait();
        }

        public static bool AddTaskToSchedule(Event @event)
        {
            CrontabSchedule schedule = CrontabSchedule.TryParse(
                    @event.EventFrequency.GetCronValue(),
                    new CrontabSchedule.ParseOptions { IncludingSeconds = true });

            if (schedule != null)
            {
                crontabSchedule.Add(@event.Id, schedule);
                crontabNextRun.Add(@event.Id, schedule.GetNextOccurrence(DateTime.UtcNow));
                //crontabNextRun.Add(@event.Id, DateTime.UtcNow.AddSeconds(10));
            }

            return true;
        }

        public static bool UpdateTaskFromSchedule(Event @event)
        {
            CrontabSchedule schedule = CrontabSchedule.TryParse(
                    @event.EventFrequency.GetCronValue(),
                    new CrontabSchedule.ParseOptions { IncludingSeconds = true });

            if (schedule != null)
            {
                if (crontabSchedule.ContainsKey(@event.Id))
                {
                    crontabSchedule[@event.Id] = schedule;
                    crontabNextRun[@event.Id] = schedule.GetNextOccurrence(DateTime.UtcNow);
                }
                else
                {
                    crontabSchedule.Add(@event.Id, schedule);
                    crontabNextRun.Add(@event.Id, schedule.GetNextOccurrence(DateTime.UtcNow));
                }
            }
            else
            {
                crontabSchedule.Remove(@event.Id);
                crontabNextRun.Remove(@event.Id);
            }

            return true;
        }

        public static bool RemoveTaskFromSchedule(Event @event)
        {            
            return crontabSchedule.Remove(@event.Id) & crontabNextRun.Remove(@event.Id);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                foreach (var schedule in crontabSchedule)
                {
                    DateTime nextRun = crontabNextRun[schedule.Key];
                    if (DateTime.UtcNow > nextRun)
                    {
                        try
                        {
                            await Process(_context.Event.Where(e => e.Id.Equals(schedule.Key)).AsNoTracking().First());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                        }
                        crontabNextRun[schedule.Key] = schedule.Value.GetNextOccurrence(DateTime.UtcNow);
                        //crontabNextRun[schedule.Key] = DateTime.UtcNow.AddSeconds(10);
                    }
                }
                await Task.Delay(5000, stoppingToken);
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        public static async Task Process(Event @event)
        {            
            if (@event.OnHold)
            {
                return;
            }
            
            string title;
            string message;

            string thingId;
            Gateway gateway;
            long locationId;

            IEnumerable<Measure> measures;

            long recentMeasuresSum, oldMeasuresSum, olderMeasuresSum, delta;

            switch (@event.EventType)
            {
                case Models.EventType.Threshold:
                    thingId = "EMSfIIoT:" + @event.Variable.Split(":").First();
                    gateway = BoschIoTSuiteApiConnector.gateways.Where(gateway => gateway.thingId.Equals(thingId)).First();
                    locationId = gateway.attributes.configuration.devices.Where(device => device.deviceId.Equals(@event.Variable)).First().locationId;

                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out measures);
                    recentMeasuresSum = measures.Sum(measure => measure.Value);

                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out measures, 1);
                    oldMeasuresSum = measures.Sum(measure => measure.Value);

                    delta = recentMeasuresSum - oldMeasuresSum;

                    title = "Device threshold";

                    switch (@event.EventValueType)
                    {
                        case EventValueType.EqualTo:
                            if (delta == @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read equaled the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        case EventValueType.GreaterThan:
                            if (delta > @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read surpassed the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        case EventValueType.GreaterThanOrEqualTo:
                            if (delta >= @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read surpassed or equaled the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        case EventValueType.LessThan:
                            if (delta < @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read falled behind the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        case EventValueType.LessThanOrEqualTo:
                            if (delta <= @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read falled behind or equaled the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        case EventValueType.NotEqualTo:
                            if (delta != @event.EventValue)
                            {
                                message = $"Your device {@event.Variable} read differed the the defined threshold {@event.EventValue}";
                                break;
                            }
                            else
                            {
                                return;
                            }
                        default:
                            return;
                    }
                    break;
                case Models.EventType.Inactive:
                    gateway = BoschIoTSuiteApiConnector.gateways.Where(gateway => gateway.thingId.Equals(@event.Variable)).First();
                    locationId = gateway.attributes.configuration.devices.First().locationId;

                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out var lastRead);
                    DateTime lastMeasureDate = lastRead.First().TimeStamp;

                    if (lastMeasureDate.AddMinutes(@event.EventValue) < DateTime.UtcNow)
                    {
                        message = $"Your gateway {@event.Variable} last communication was {Math.Floor(DateTime.UtcNow.Subtract(lastMeasureDate).TotalMinutes)} minutes ago";
                        title = "Gateway inactive";
                        break;
                    }
                    else
                    {
                        return;
                    }
                case Models.EventType.Algorithm:
                    thingId = "EMSfIIoT:" + @event.Variable.Split(":").First();
                    gateway = BoschIoTSuiteApiConnector.gateways.Where(gateway => gateway.thingId.Equals(thingId)).First();
                    locationId = gateway.attributes.configuration.devices.Where(device => device.deviceId.Equals(@event.Variable)).First().locationId;

                    double percentage;
                    double multiplier;
                    string timeSpan;

                    switch (@event.EventFrequency)
                    {
                        case EventFrequency.Every15Minutes:
                            multiplier = 1/4.0;
                            timeSpan = "15 minutes";
                            break;
                        case EventFrequency.Every30Minutes:
                            multiplier = 1/2.0;
                            timeSpan = "30 minutes";
                            break;
                        case EventFrequency.EveryHour:
                            multiplier = 1;
                            timeSpan = "hour";
                            break;
                        case EventFrequency.Every6Hours:
                            multiplier = 6;
                            timeSpan = "6 hours";
                            break;
                        case EventFrequency.EveryDay:
                            multiplier = 24;
                            timeSpan = "day";
                            break;
                        case EventFrequency.EveryWeek:
                            multiplier = 24 * 7;
                            timeSpan = "week";
                            break;
                        default:
                            return;
                    }

                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out measures);
                    recentMeasuresSum = measures.Sum(measure => measure.Value);

                    MeasuresApiConnector.GetDeviceMeasures(locationId, out measures, Convert.ToInt32(multiplier * 60));

                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out measures, measures.Count());
                    oldMeasuresSum = measures.Sum(measure => measure.Value);

                    MeasuresApiConnector.GetDeviceMeasures(locationId, out measures, Convert.ToInt32(2* multiplier * 60));
                    
                    MeasuresApiConnector.GetDeviceLastMeasures(locationId, out measures, measures.Count());
                    olderMeasuresSum = measures.Sum(measure => measure.Value);

                    if (oldMeasuresSum != olderMeasuresSum)
                    {
                        percentage = Convert.ToDouble(recentMeasuresSum - oldMeasuresSum) / Convert.ToDouble(oldMeasuresSum - olderMeasuresSum);
                    }
                    else
                    {
                        percentage = double.PositiveInfinity;
                    }

                    if (Math.Abs(percentage) > @event.EventValue)
                    {
                        message = $"Your device {@event.Variable} read from the last {timeSpan} differed the previous one more than the defined threshold of {@event.EventValue}%. Difference was {percentage}%.";
                    }
                    else
                    {
                        return;
                    }

                    title = "Device threshold";

                    break;
                default:
                    return;
            }
            await SendNotificationToUser("APINotification", title, message, @event);


            await _events.PutEventOnHold(@event.Id);
        }

        private static async Task SendNotification(string type, string title, string message)
        {
            foreach (var user in GraphApiConnector.users)
            {
                await _notifications.PostNotifications(new NotificationDTO
                {
                    Type = type,
                    Title = title,
                    Description = message,
                    Origin = "API",
                    Destination = user.Id
                });

                await SendGridAPIConnector.SendEmail(
                    new SendGrid.Helpers.Mail.EmailAddress(user.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).FirstOrDefault().IssuerAssignedId, user.DisplayName),
                    title,
                    message,
                    message
                );
            }

            await _notificationsHub.Clients.All.SendAsync("Notification", type, title, "API", message);
        }

        private static async Task SendNotificationToUser(string type, string title, string message, Event @event)
        {                       
            if (@event.NotificationType != 0)
            {
                await _notifications.PostNotifications(new NotificationDTO
                {
                    Type = type,
                    Title = title,
                    Description = message,
                    Origin = "API",
                    Destination = @event.Username.ToString()
                });
            }

            User user = GraphApiConnector.users.Where(user => user.Id.Equals(@event.Username.ToString())).First();

            if (@event.NotificationType.HasFlag(NotificationType.Email))
            {
                await SendGridAPIConnector.SendEmail(
                        new SendGrid.Helpers.Mail.EmailAddress(user.Identities.Where(identity => identity.SignInType.Equals("emailAddress")).FirstOrDefault().IssuerAssignedId, user.DisplayName),
                        title,
                        message,
                        message
                    );
            }

            if (@event.NotificationType.HasFlag(NotificationType.PopUp))
            {
                await _notificationsHub.Clients.User(@event.Username.ToString()).SendAsync("Notification", type, title, "API", message);
            }

            if (@event.NotificationType.HasFlag(NotificationType.Telegram))
            {
                await TelegramAPIConnector.SendMessageToChat(user.AdditionalData["extension_257f2369f5054b62a0f21c2c82ad96fc_TelegramChatId"].ToString(), message);
            }
        }

        /*private void CreateTestMessage()
        {
            string to = "";
            string from = "";
            MailMessage message = new MailMessage(from, to)
            {
                Subject = "EMSfIIoT API",
                Body = @"Using this new feature, you can send an email message from an application very easily. \r\n " + DateTime.UtcNow.ToString("F")
            };
            SmtpClient client = new SmtpClient
            {
                Credentials = new NetworkCredential(from, ""),
                Port = 587,
                Host = "smtp.office365.com",
                EnableSsl = true
            };

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception caught in CreateTestMessage(): {0}",
                    ex.ToString());
            }
        }*/
    }
}
