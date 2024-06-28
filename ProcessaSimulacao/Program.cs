using System.Text;
using System.Text.Json;
using API;
using Microsoft.AspNetCore.SignalR.Client;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory() { HostName = "localhost" };

LeFilaProcessar(factory);

void SendMessageToQueue(ConnectionFactory factory, string message, string fila)
{
	using (var connection = factory.CreateConnection())
	using (var channelProducer = connection.CreateModel())
	{
		channelProducer.QueueDeclare(queue: fila, durable: false, exclusive: false, autoDelete: false, arguments: null);

		var body = Encoding.UTF8.GetBytes(message);

		channelProducer.BasicPublish(exchange: "", routingKey: fila, basicProperties: null, body: body);
		Console.WriteLine($"Resultado enviado!");
	}
}

async Task EnviaMensagemWebSocket(string urlHub, string funcEnviar, string mensagem)
{
	var connection = new HubConnectionBuilder()
		   .WithUrl(urlHub)
		   .Build();

	try
	{
		await connection.StartAsync();

		await connection.InvokeAsync(funcEnviar, mensagem);
	}
	catch
	{
		Console.WriteLine("não foi possivel conectar.");

	}

}

List<ItemParametro> LerFila(ConnectionFactory factory, string fila, long simulacaoId)
{
	var itensDevolverFila = new List<string>();
	var itensExibir = new List<ItemParametro>();

	using var connectionConsumer = factory.CreateConnection();
	using var channelConsumer = connectionConsumer.CreateModel();

	channelConsumer.QueueDeclare(queue: fila, durable: false, exclusive: false, autoDelete: false, arguments: null);
	while (true)
	{
		var result = channelConsumer.BasicGet(queue: fila, autoAck: true);
		if (result != null)
		{
			var body = result.Body.ToArray();
			var message = Encoding.UTF8.GetString(body);
			var itemParamentro = JsonSerializer.Deserialize<ItemParametro>(message);
			if (itemParamentro != null)
			{
				if (itemParamentro.SimulacaoId == simulacaoId) itensExibir.Add(itemParamentro);
				else itensDevolverFila.Add(JsonSerializer.Serialize(itemParamentro));
			}
		}
		else
		{
			break;
		}

	};

	foreach (var item in itensDevolverFila)
	{
		SendMessageToQueue(factory, item, fila);
	}
	return itensExibir;

}

void LeFilaProcessar(ConnectionFactory factory)
{
	var connectionConsumer = factory.CreateConnection();
	var channelConsumer = connectionConsumer.CreateModel();

	channelConsumer.QueueDeclare(queue: "Processar", durable: false, exclusive: false, autoDelete: false, arguments: null);

	var consumer = new EventingBasicConsumer(channelConsumer);
	consumer.Received += async (model, ea) =>
	{
		var body = ea.Body.ToArray();
		var message = Encoding.UTF8.GetString(body);

		await ProcessaSimulacao(message, new List<string>());
	};


	channelConsumer.BasicConsume(queue: "Processar", autoAck: true, consumer: consumer);

	Console.WriteLine("[enter] para sair");
	Console.ReadLine();
}

async Task ProcessaSimulacao(string message, List<string> itens)
{
	var itemProcessar = JsonSerializer.Deserialize<ItemProcessar>(message);
	if (itemProcessar != null)
	{
		var runSimulation = true;
		Console.WriteLine($"item a ser processado recebido!");
		itens.Add(message);
		var tentativas = 0;
		var itemResposta = new ItemResultado
		{
			SimulacaoId = itemProcessar.SimulacaoId,
			LogsJson = JsonSerializer.Serialize(new
			{
				Regra = itemProcessar.RegraId,
				Gatilho = itemProcessar.Gatilho,
				Status = "Processado"
			}),
			SimulacaoJson = JsonSerializer.Serialize(new
			{
				Comandos = string.Join(",", itemProcessar.Comandos)
			})
		};
		while (runSimulation)
		{


			Console.WriteLine("processando");
			await EnviaMensagemWebSocket("https://localhost:7284/signalrhub", "SendMessage", $"TesteEnvio: {DateTime.Now.ToString("G")}");
			Thread.Sleep(1000);
			//modificar item aqui

			tentativas++;

			//le fila de parametros para alterar o objeto
			var novosParametros = LerFila(factory, "Parametros", itemProcessar.SimulacaoId);
			if (novosParametros.Count() > 0) Console.WriteLine("novos parametros recebidos");
			else Console.WriteLine("nenhum paramentro novo");
			if (tentativas == 100) runSimulation = false;
		}
		Console.WriteLine("---Item Processado---");
		SendMessageToQueue(factory, JsonSerializer.Serialize(itemResposta), "Resultados");
	}

	Console.WriteLine($"qtdItensProcessados:  {itens.Count()}");

}


//melhorias:
// - criar biblioteca de classe para utilizar como dll no simulacao) com:
//	- interface com os metodos em comum com metodos genericos.
//  - implementacao Base da interface.
//	- classes em comum que precisam ser iguais para deserializar.
//	 - BaseFila com propriedades em comum entre todas as classes(simulacaoId?)
//   - ItemProcessar
//   - ItemResultado
//   - ItemParametro