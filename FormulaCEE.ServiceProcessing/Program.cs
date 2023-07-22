// See https://aka.ms/new-console-template for more information
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("Isso é um teste");

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

var consumer = new EventingBasicConsumer(channel);

consumer.Received += (model, EventArgs) =>
{
    var body = EventArgs.Body.ToArray();

    var message = Encoding.UTF8.GetString(body);

    Console.WriteLine($"Processo de solicitação iniciado - {message}");
};

channel.BasicConsume("Solicitacoes", true, consumer);

Console.ReadKey();