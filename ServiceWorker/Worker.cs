namespace ServiceWorker;
using System.IO;
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
        // TODO
        // Ændre "DESKTOP" til "FilePath"
        _filePath = config["FilePath"] ?? "/srv";

        _logger.LogInformation($"Filepath: {_filePath}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                // 172.17.0.2
                HostName = "172.17.0.2"
            };
            
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            channel.QueueDeclare(queue: "hello",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            Console.WriteLine(" [*] Waiting for messages.");

            var consumer = new EventingBasicConsumer(channel);

            // Delegate method
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);

                PlanDTO? plan = JsonSerializer.Deserialize<PlanDTO>(message);

                Console.WriteLine($"Plan modtaget:\nKundenavn: {plan.KundeNavn}\nStarttidspunkt: {plan.StartTidspunkt}\nStartsted: {plan.StartSted}\nEndested: {plan.SlutSted}");


                // Tjekker om filen eksisterer og om den er tom
                if (!File.Exists(Path.Combine(_filePath, "planliste.csv")) || new FileInfo(Path.Combine(_filePath, "planliste.csv")).Length == 0)
                {
                    // Skriver headeren til .CSV-filen
                    using (StreamWriter outputFile = new StreamWriter(Path.Combine(_filePath, "planListe.csv")))
                    {
                        outputFile.WriteLine("Kundenavn,Starttidspunkt,Startsted,Slutsted");
                        outputFile.Close();
                    }
                }

                // StreamWriter til at sende skrive i .CSV-filen
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(_filePath, "planListe.csv"), true))
                {

                    outputFile.WriteLineAsync($"{plan.KundeNavn},{plan.StartTidspunkt},{plan.StartSted},{plan.SlutSted}");

                    outputFile.Close();
                }

            };

            channel.BasicConsume(queue: "hello",
                                 autoAck: true,
                                 consumer: consumer);


            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
        }

    }
}
