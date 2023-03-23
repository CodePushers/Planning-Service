using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ServiceWorker.Test;

public class Tests
{
    private readonly ILogger<Worker> _logger;
    private Worker _worker;

    [SetUp]
    public void Setup()
    {
        var logger = Substitute.For<ILogger<Worker>>();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "FilePath", "/Users/jacobkaae/Desktop/Test" },
            { "HostnameRabbit", "localhost" }
        }).Build();


        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "hello",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        var planDTO = new PlanDTO
        {
            KundeNavn = "Anders",
            SlutSted = "Trapkasgade",
            StartSted = "Hos Rikke",
            StartTidspunkt = DateTime.Parse("2019-08-01")
        };

        string message = JsonSerializer.Serialize(planDTO);

        var body = Encoding.UTF8.GetBytes(message);

        channel.BasicPublish(exchange: string.Empty,
                             routingKey: "hello",
                             basicProperties: null,
                             body: body);

    }

    [Test]
    public void Test1()
    {
        
        
    }
}