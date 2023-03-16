using System.Numerics;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using shared;

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


Console.WriteLine($"Plan sendt:\nKundenavn: {planDTO.KundeNavn}\nStarttidspunkt: {planDTO.StartTidspunkt}\nStartsted: {planDTO.StartSted}\nEndested: {planDTO.SlutSted}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();