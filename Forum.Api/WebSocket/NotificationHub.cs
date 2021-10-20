using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ForumJV.WebSocket
{
    public class NotificationHub : Hub
    {
        public Task SendNotification(string userId)
        {
            throw new NotImplementedException();
        }
    }
}