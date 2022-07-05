using GeekShopping.PaymentAPI.Messages;
using GeekShopping.PaymentAPI.RabbitMqSender;
using GeekShopping.PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace GeekShopping.PaymentAPI.MessageConsumer
{
    public class RabbitMqPaymentConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private IRabbitMqMessageSender _rabbitMqMessageSender;
        private readonly IProcessPayment _processPayment;

        public RabbitMqPaymentConsumer(IProcessPayment processPayment, IRabbitMqMessageSender rabbitMqMessageSender)
        {
            _processPayment = processPayment;
            _rabbitMqMessageSender = rabbitMqMessageSender;

            var factory = new ConnectionFactory()
            {
                HostName = "localhost",
                UserName = "guest",
                Password = "guest"
            };
            _connection = factory.CreateConnection();

            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "orderpaymentprocessqueue", false, false, false, arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (channel, evt) =>
            {
                var content = Encoding.UTF8.GetString(evt.Body.ToArray());
                PaymentMessage paymentMessage = JsonSerializer.Deserialize<PaymentMessage>(content);
                ProcessPayment(paymentMessage).GetAwaiter().GetResult();
                _channel.BasicAck(evt.DeliveryTag, false);
            };
            _channel.BasicConsume("orderpaymentprocessqueue", false, consumer);
            return Task.CompletedTask;
        }

        private async Task ProcessPayment(PaymentMessage paymentMessage)
        {
            var result = _processPayment.PaymentProcessor();

            UpdatePaymentResultMessage paymentResult = new UpdatePaymentResultMessage()
            {
                Status = result,
                OrderId = paymentMessage.OrderId,
                Email = paymentMessage.Email
            };

            try
            {
                _rabbitMqMessageSender.SendMessage(paymentResult, "orderpaymentresultqueue");
            }
            catch (Exception)
            {
                //Log Exception
                throw;
            }
        }
    }
}
