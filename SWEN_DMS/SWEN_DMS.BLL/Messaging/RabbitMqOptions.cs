namespace SWEN_DMS.BLL.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string User { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "dms.exchange";
    public string Queue { get; set; } = "ocr.requests";
    public string RoutingKey { get; set; } = "ocr.request";
}