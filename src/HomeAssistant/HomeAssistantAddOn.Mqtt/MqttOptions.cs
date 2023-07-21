namespace HomeAssistantAddOn.Mqtt;
public class MqttOptions
{

    public string Host { get; set; } = default!;

    public int Port { get; set; } = 1883;

    public string? Id { get; set; } = null;

    public string? Pw { get; set; } = null;

    public bool Tls { get; set; } = false;
}