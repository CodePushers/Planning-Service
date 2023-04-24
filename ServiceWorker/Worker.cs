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
    private readonly string _hostName;

    public Worker(ILogger<Worker> logger, IConfiguration config)
    {
        _logger = logger;
        // Henter miljø variabel "FilePath" og "HostnameRabbit" fra docker-compose
        _filePath = config["FilePath"] ?? "/srv";
        _hostName = config["HostnameRabbit"];
    
        _logger.LogInformation($"Filepath: {_filePath}");
        _logger.LogInformation($"Connection: {_hostName}");

        var hostName = System.Net.Dns.GetHostName();
        var ips = System.Net.Dns.GetHostAddresses(hostName);
        var _ipaddr = ips.First().MapToIPv4().ToString();
        _logger.LogInformation(1, $"PlanningService responding from {_ipaddr}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var factory = new ConnectionFactory
        {
            HostName = _hostName
        };
        
        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        // Opretter en kø "hello" hvis den ikke allerede findes i vores rabbitmq-server
        channel.QueueDeclare(queue: "hello",
                                durable: false,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

        _logger.LogInformation("[*] Waiting for messages.");

        var consumer = new EventingBasicConsumer(channel);

        // Delegate method
        consumer.Received += (model, ea) =>
        {
            // Henter data ned fra køen
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            // Deserialiserer det indsendte data om til C# objekt
            PlanDTO? plan = JsonSerializer.Deserialize<PlanDTO>(message);

            _logger.LogInformation($"[*] Plan modtaget:\n\tKundenavn: {plan.KundeNavn}\n\tStarttidspunkt: {plan.StartTidspunkt}\n\tStartsted: {plan.StartSted}\n\tEndested: {plan.SlutSted}");

            // Tjekker om filen eksisterer og om den er tom
            if (!File.Exists(Path.Combine(_filePath, "planListe.csv")) || new FileInfo(Path.Combine(_filePath, "planListe.csv")).Length == 0)
            {
                _logger.LogInformation("Ny planListe.csv fil oprettet: {0}", DateTime.Now);
                // Laver en ny "planListe.csv" fil på stien
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(_filePath, "planListe.csv")))
                {
                    // Opretter headeren i filen og lukker den
                    outputFile.WriteLine("Kundenavn,Starttidspunkt,Startsted,Slutsted");
                    outputFile.Close();
                }
            }

            // StreamWriter til at sende skrive i .CSV-filen
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(_filePath, "planListe.csv"), true))
            {
                _logger.LogInformation("Ny booking skrevet i planListe.csv");
                // Laver en ny linje med det tilsendte data og lukker filen.
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
}
