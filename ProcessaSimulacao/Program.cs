using System.Text;
using System.Text.Json;
using API;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory() { HostName = "localhost" };

// Consumir mensagens da fila "a"
var connectionConsumer = factory.CreateConnection();
var channelConsumer = connectionConsumer.CreateModel();

channelConsumer.QueueDeclare(queue: "Processar", durable: false, exclusive: false, autoDelete: false, arguments: null);

var listaItens = new List<string>();
var consumer = new EventingBasicConsumer(channelConsumer);
consumer.Received += (model, ea) =>
{
	var body = ea.Body.ToArray();
	var message = Encoding.UTF8.GetString(body);

	var itemProcessar = JsonSerializer.Deserialize<ItemProcessar>(message);
	if (itemProcessar != null)
	{
		Console.WriteLine($"item a ser processado recebido!");
		listaItens.Add(message);
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
		while (tentativas < 100)
		{


			Console.WriteLine("processando");
			Thread.Sleep(1000);
			//modificar item aqui

			tentativas++;

			//le fila de parametros para alterar o objeto
		}
		Console.WriteLine("---Item Processado---");
		SendMessageToQueue(factory, JsonSerializer.Serialize(itemResposta));
	}

	Console.WriteLine($"qtdItensProcessados:  {listaItens.Count()}");
};


channelConsumer.BasicConsume(queue: "Processar", autoAck: true, consumer: consumer);

Console.WriteLine("[enter] para sair");
Console.ReadLine();


static void SendMessageToQueue(ConnectionFactory factory, string message)
{
	using (var connection = factory.CreateConnection())
	using (var channelProducer = connection.CreateModel())
	{
		channelProducer.QueueDeclare(queue: "Resultados", durable: false, exclusive: false, autoDelete: false, arguments: null);

		var body = Encoding.UTF8.GetBytes(message);

		channelProducer.BasicPublish(exchange: "", routingKey: "Resultados", basicProperties: null, body: body);
		Console.WriteLine($"Resultado enviado!");
	}
}