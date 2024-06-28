using Microsoft.AspNetCore.SignalR;

namespace ProcessaSimulacao
{
	public class NotificationService
	{
		private readonly IHubContext<DemoHub> _hubContext;

		public NotificationService(IHubContext<DemoHub> hubContext)
		{
			_hubContext = hubContext;
		}

		public async Task EnviaNotificacao(string mensagem)
		{
			await _hubContext.Clients.All.SendAsync("ReceiveNotification", "Id", mensagem);
		}
	}
}
