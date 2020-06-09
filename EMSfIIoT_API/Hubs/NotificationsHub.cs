using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

using EMSfIIoT_API.DbContexts;
using EMSfIIoT_API.Models;
using EMSfIIoT_API.Controllers;
using Microsoft.AspNetCore.Mvc;
using SharedAPI;
using Microsoft.EntityFrameworkCore;

namespace EMSfIIoT_API.Hubs
{
    [Authorize(AuthenticationSchemes = "bearerAuth")]
    public class NotificationsHub : Hub
    {
        private readonly ApiDbContext _context;
        private readonly NotificationsController _notifications;

        public NotificationsHub(ApiDbContext context)
        {
            _context = context;
            _notifications = new NotificationsController(_context);
        }
        
        public async Task SendMessage(string type, string title, string message)
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
            }           
            
            await Clients.All.SendAsync("Notification", type, title, "API", message);
        }

        public async Task SendMessageToUser(string type, string title, string to, string message)
        {
            var recipient = GraphApiConnector.users.FirstOrDefault(user =>
                user.Identities.FirstOrDefault(identity => 
                identity.SignInType.Equals("emailAddress")).IssuerAssignedId.ToString().Trim().ToLower().Equals(to.Trim().ToLower()));

            if (recipient != null)
            {
                var result = await _notifications.PostNotifications(new NotificationDTO
                {
                    Type = type,
                    Title = title,
                    Description = message,
                    Origin = Context.UserIdentifier,
                    Destination = recipient.Id
                });

                CreatedAtActionResult result1 = (CreatedAtActionResult)result.Result;
                Notification notification = (Notification)result1.Value;

                await Clients.User(recipient.Id).SendAsync("Notification", type, title, Context.User.Identity.Name, message);
                await Clients.User(Context.UserIdentifier).SendAsync("Notification", "MessageSuccess", "Message sent!", "API", "Your message was sent to user " + recipient.DisplayName);
            }
            else
            {
                await Clients.User(Context.UserIdentifier).SendAsync("Notification", "MessageError", "User not found!", "API", "User with email " + to + " not found");
            }
        }

        public async Task MarkNotificationsAsRead()
        {
            var notifications = await _context.Notifications
            .Where(notification => notification.Destination.ToString().Equals(Context.User.FindFirst(ClaimTypes.NameIdentifier).Value) & !notification.ReadAt.HasValue)
            .ToListAsync();
            notifications.ForEach(notification => notification.ReadAt = DateTime.Now);

            await _context.SaveChangesAsync();
        }

        public async Task AddToGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has joined the group {groupName}.");
        }

        public async Task RemoveFromGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            await Clients.Group(groupName).SendAsync("Send", $"{Context.ConnectionId} has left the group {groupName}.");
        }
    }
}