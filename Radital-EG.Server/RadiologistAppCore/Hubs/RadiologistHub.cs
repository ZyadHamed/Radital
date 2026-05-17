using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace RadiologistAppCore.Hubs
{
    public class RadiologistHub : Hub
    {
        public async Task JoinRadiologistGroup(string radiologistId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"radiologist-{radiologistId}");
            Console.WriteLine($"[Hub] Client joined group: radiologist-{radiologistId}");
        }
    }
}
