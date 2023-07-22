using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace FormulaCEE.API.Services;

public class MessageProducer : IMessageProducer
{
    public void EnviarMensagem<T>(T mensagem)
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost",
            UserName = "user",
            Password = "mypass",
            VirtualHost = "/"
        };

        var conn = factory.CreateConnection();

        using var channel = conn.CreateModel();

        channel.QueueDeclare("Solicitacoes", durable: true, exclusive: false);

        var jsonString = JsonSerializer.Serialize(mensagem);
        var body = Encoding.UTF8.GetBytes(jsonString);

        channel.BasicPublish("", "Solicitacoes", body: body);
    }
}