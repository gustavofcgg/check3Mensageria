using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;


    var factory = new ConnectionFactory() { HostName = "localhost" };
    using (var connection = factory.CreateConnection())
    using (var channel = connection.CreateModel())
{
        channel.QueueDeclare(queue: "estoque",
                             durable: false,
                             exclusive: false,
                             autoDelete: false,
                             arguments: null);

        
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var mensagem = Encoding.UTF8.GetString(body);
            Console.WriteLine($"Recebido: {mensagem} e atualizado no estoque");
        };

        channel.BasicConsume(queue: "fila_teste",
                             autoAck: true,
                             consumer: consumer);

        Console.WriteLine("Aguardando mensagens...");
        Console.ReadLine();
}
