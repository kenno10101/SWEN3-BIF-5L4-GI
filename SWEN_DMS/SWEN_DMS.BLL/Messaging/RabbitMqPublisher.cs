using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SWEN_DMS.BLL.Interfaces;

namespace SWEN_DMS.BLL.Messaging;

public sealed class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    private readonly RabbitMqOptions _options;
    private readonly IConnection _connection;

    public RabbitMqPublisher(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = _options.Host,
            Port = _options.Port,
            UserName = _options.User,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost,
            DispatchConsumersAsync = true,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        };

        _connection = factory.CreateConnection("swen-dms-publisher");
        EnsureTopology();
    }

    private void EnsureTopology()
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(exchange: _options.Exchange, type: ExchangeType.Direct, durable: true, autoDelete: false);
        channel.QueueDeclare(queue: _options.Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(queue: _options.Queue, exchange: _options.Exchange, routingKey: _options.RoutingKey);
    }

    public Task PublishAsync<T>(T payload, string? routingKeyOverride = null)
    {
        using var channel = _connection.CreateModel();
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";

        var json = JsonSerializer.Serialize(payload);
        var body = Encoding.UTF8.GetBytes(json);

        var rk = routingKeyOverride ?? _options.RoutingKey;
        channel.BasicPublish(exchange: _options.Exchange, routingKey: rk, basicProperties: props, body: body);
        return Task.CompletedTask;
    }

    public void Dispose() => _connection?.Dispose();
}
