using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;

namespace HomeAssistantAddOn.Mqtt;
public class MqttService : IDisposable
{
    private readonly ILogger<MqttService> _logger;
    private readonly IManagedMqttClient _mqttClient;
    private readonly ManagedMqttClientOptions _mqttOption;
    private readonly ConcurrentDictionary<string, List<Func<string, Task>>> _subscriptions = new();

    public MqttService(
        ILogger<MqttService> logger,
        IOptionsMonitor<MqttOptions> optionsMonitor)
    {
        _logger = logger;

        var mqttFactory = new MqttFactory();
        _mqttClient = mqttFactory.CreateManagedMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += ApplicationMessageReceivedAsync;
        _mqttOption = new ManagedMqttClientOptionsBuilder()
            .WithAutoReconnectDelay(TimeSpan.FromSeconds(10))
            .WithClientOptions((builder) =>
            {
                var options = optionsMonitor.CurrentValue;
                builder
                    .WithTcpServer(options.Host, options.Port)
                    .WithClientId(Assembly.GetEntryAssembly()!.FullName);
                if (!string.IsNullOrEmpty(options.Id))
                {
                    builder.WithCredentials(options.Id, string.IsNullOrEmpty(options.Pw) ? null : options.Pw);
                }
                if (options.Tls)
                {
                    builder.WithTls();
                }
            })
            .Build();
    }

    public async Task StartAsync()
    {
        await _mqttClient.StartAsync(_mqttOption);
        _logger.LogDebug("Start");
    }

    public async Task StopAsync()
    {
        await _mqttClient.StopAsync();
        _logger.LogDebug("Stop");
    }

    public void Subscribe(string topic, Func<string, Task> subscribeTask)
    {
        _subscriptions.AddOrUpdate(topic
            , _ =>
            {
                _mqttClient.SubscribeAsync(topic);
                return new List<Func<string, Task>> { subscribeTask };
            }
            , (_, list) =>
            {
                list.Add(subscribeTask);
                return list;
            });
        _logger.LogDebug("Subscribe {topic}", topic);
    }

    private async Task ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        if (_subscriptions.TryGetValue(args.ApplicationMessage.Topic, out var subscribeTasks))
        {
            var payload = args.ApplicationMessage.ConvertPayloadToString();
            await Task.WhenAll(subscribeTasks.Select(subscribeTask => subscribeTask.Invoke(payload)));
        }
        _logger.LogDebug("Receive {topic}", args.ApplicationMessage.Topic);
    }

    private static readonly JsonSerializerSettings MqttPaloadSerializerSetting = new() { NullValueHandling = NullValueHandling.Ignore };

    public async Task PublishAsync(string topic, object payload, bool retain = false)
    {
        await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonConvert.SerializeObject(payload, MqttPaloadSerializerSetting))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(retain)
            .Build());
        _logger.LogDebug("Publish {topic}", topic);
    }

    public void Dispose()
    {
        _mqttClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}