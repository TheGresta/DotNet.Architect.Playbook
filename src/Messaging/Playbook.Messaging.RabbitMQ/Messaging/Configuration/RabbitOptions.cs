namespace Playbook.Messaging.RabbitMQ.Messaging.Configuration;

public class RabbitOptions
{
    public string HostName { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; } = "/";
    public int Port { get; set; } = 5672;
    public int MaxConcurrency { get; set; } = 10; // The "Cap" for your multi-thread requirement
    public int PrefetchCount { get; set; } = 20; // Backpressure control
    public int ChannelPoolSize { get; set; } = 5; // How many channels to keep warm
}
