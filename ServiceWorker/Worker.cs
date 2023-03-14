namespace ServiceWorker;

using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    private readonly string _filePath;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        _filePath = config["FilePath"] ?? "/srv";

        _logger.LogInformation($"Filepath: {_filePath}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: "farvel",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        Console.WriteLine(" [*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            PlanDTO? plan = JsonSerializer.Deserialize<PlanDTO>(message);

            Console.WriteLine($"Plan modtaget:\nKundenavn: {plan.KundeNavn}\nStarttidspunkt: {plan.StartTidspunkt}\nStartsted: {plan.StartSted}\nEndested: {plan.SlutSted}");
        };

        channel.BasicConsume(queue: "farvel",
                             autoAck: true,
                             consumer: consumer);


        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            await Task.Delay(1000, stoppingToken);
        }
    }
}
