using Microsoft.AspNetCore.SignalR;

namespace ProcessaSimulacao
{
	public class DemoHub : Hub
	{
		public async Task SendNotification(string user, string message)
		{
			await Clients.All.SendAsync("ReceiveNotification", user, message);
		}
	}
}
