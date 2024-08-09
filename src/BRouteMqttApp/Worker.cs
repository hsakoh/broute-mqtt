using BRouteController;
using HomeAssistantAddOn.Mqtt;

namespace BRouteMqttApp;

public class Worker(
    ILogger<Worker> logger
        , BRouteControllerService bRouteControllerService
        , MqttService mqttService
        ) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await mqttService.StartAsync();
        await bRouteControllerService.InitalizeAsync(cancellationToken);

        await PublishDeviceConfigsAsync();

        await Task.Delay(5 * 1000, cancellationToken);

        await PublishDeviceActiveStatusAsync();
        await PublishDevicePassiveStatusAsync();
        await PublishDeviceStaticStatusAsync();
        SubscribeCommandTopic();

        bRouteControllerService.ActivePropertiesReadedCallback = PublishDeviceActiveStatusAsync;
        bRouteControllerService.PassivePropertiesReadedCallback = PublishDevicePassiveStatusAsync;
        bRouteControllerService.PassivePropertiesOnTimeCallback = PublishDevicePassiveOnTimeStatusAsync;

        await base.StartAsync(cancellationToken);
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await bRouteControllerService.PollAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await mqttService.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    #region Configure Senser
    private async Task PublishDeviceConfigsAsync()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        await PublishSensorConfigAsync(serial, "placement", "設置場所", "static", icon: "mdi:map-marker");
        await PublishSensorConfigAsync(serial, "version", "規格Version情報", "static", icon: "mdi:information");
        await PublishSensorConfigAsync(serial, "makercode", "メーカコード", "static", icon: "mdi:factory");
        await PublishSensorConfigAsync(serial, "serialnumber", "製造番号", "static", icon: "mdi:identifier");

        await PublishSensorConfigAsync(serial, "cumulative_normal", "積算電力量計測値(正方向)", "passive"
            , device_class: "energy", state_class: "total_increasing", unit_of_measurement: "kWh");
        await PublishSensorConfigAsync(serial, "cumulative_reverse", "積算電力量計測値(逆方向)", "passive"
            , device_class: "energy", state_class: "total_increasing", unit_of_measurement: "kWh");
        await PublishSensorConfigAsync(serial, "passive_timestamp", "更新日時(積算電力量)", "passive"
            , device_class: "timestamp", value_template: "{% set ts = value_json.get('timestamp', {})  %} {% if ts %}\n  {{ (ts / 1000) | timestamp_local | as_datetime }}\n{% else %}\n  {{ this.state }}\n{% endif %}");


        await PublishSensorConfigAsync(serial, "instantaneous_current_r", "瞬時電流計測値(R相)", "active"
            , device_class: "current", state_class: "measurement", unit_of_measurement: "A");
        await PublishSensorConfigAsync(serial, "instantaneous_current_t", "瞬時電流計測値(T相)", "active"
            , device_class: "current", state_class: "measurement", unit_of_measurement: "A");
        await PublishSensorConfigAsync(serial, "instantaneous_electric_power", "瞬時電力計測値", "active"
            , device_class: "apparent_power", state_class: "measurement", unit_of_measurement: "W");
        await PublishSensorConfigAsync(serial, "active_timestamp", "更新日時(瞬時値)", "active"
            , device_class: "timestamp", value_template: "{% set ts = value_json.get('timestamp', {})  %} {% if ts %}\n  {{ (ts / 1000) | timestamp_local | as_datetime }}\n{% else %}\n  {{ this.state }}\n{% endif %}");

        await SendButtonConfigAsync(serial, "active", "瞬時値の取得", "update");
        await SendButtonConfigAsync(serial, "passive", "積算電力量の取得", "update");

    }

    private async Task PublishSensorConfigAsync(
        string serial, string type, string name, string subTopic
        , string? icon = null
        , string? device_class = null
        , string? state_class = null
        , string? unit_of_measurement = null, string? value_template = null)
    {
        var payload = new
        {
            icon,
            name,
            state_topic = $"homeassistant/sensor/{serial}/state/{subTopic}",
            unit_of_measurement,
            state_class,
            device_class,
            value_template = value_template ?? $"{{{{value_json.{type}}}}}",
            unique_id = $"{type}_{serial}",
            object_id = $"{type}_{serial}",
            device = new
            {
                identifiers = new[] { $"smart_meter_{serial}" },
                name = nameof(低圧スマート電力量メータ),
            },
        };
        await mqttService.PublishAsync($"homeassistant/sensor/{type}_{serial}/config", payload, true);
    }

    private async Task SendButtonConfigAsync(string serial, string type, string name, string device_class)
    {
        var payload = new
        {
            device_class,
            name,
            command_topic = $"homeassistant/button/{serial}/cmd",
            payload_press = type,
            unique_id = $"btn_{type}_{serial}",
            object_id = $"btn_{type}_{serial}",
            device = new
            {
                identifiers = new[] { $"smart_meter_{serial}" },
                name = nameof(低圧スマート電力量メータ),
            },
        };
        await mqttService.PublishAsync($"homeassistant/button/btn_{type}_{serial}/config", payload, true);
    }
    #endregion

    #region Notifiy Senser Stauts

    public async Task PublishDeviceStaticStatusAsync()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        await SendSensorStateAsync(serial, "static", new
        {
            placement = bRouteControllerService.Meter.設置場所,
            version = bRouteControllerService.Meter.規格Version情報,
            makercode = bRouteControllerService.Meter.メーカコード,
            serialnumber = serial,
        });
        logger.LogInformation("ステータス(静的)通知 {a},{b},{c},{d}",
            bRouteControllerService.Meter.設置場所,
            bRouteControllerService.Meter.規格Version情報,
            bRouteControllerService.Meter.メーカコード,
            serial
            );
    }
    public async Task PublishDeviceActiveStatusAsync()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        await SendSensorStateAsync(serial, "active", new
        {
            instantaneous_current_r = bRouteControllerService.Meter.瞬時電流計測値?.r,
            instantaneous_current_t = bRouteControllerService.Meter.瞬時電流計測値?.t,
            instantaneous_electric_power = bRouteControllerService.Meter.瞬時電力計測値,
            timestamp = bRouteControllerService.Meter.現在年月日時刻
        });
        logger.LogInformation("ステータス(瞬時)通知 {r}A,{t}A,{e}W,{time}",
            bRouteControllerService.Meter.瞬時電流計測値?.r,
            bRouteControllerService.Meter.瞬時電流計測値?.t,
            bRouteControllerService.Meter.瞬時電力計測値,
            bRouteControllerService.Meter.現在年月日時刻
            );
    }
    public async Task PublishDevicePassiveStatusAsync()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        await SendSensorStateAsync(serial, "passive", new
        {
            cumulative_normal = bRouteControllerService.Meter.積算電力量計測値_正方向計測値,
            cumulative_reverse = bRouteControllerService.Meter.積算電力量計測値_逆方向計測値,
            timestamp = bRouteControllerService.Meter.現在年月日時刻
        });
        logger.LogInformation("ステータス(積算)通知 {n}W,{r}W,{time}",
            bRouteControllerService.Meter.積算電力量計測値_正方向計測値,
            bRouteControllerService.Meter.積算電力量計測値_逆方向計測値,
            bRouteControllerService.Meter.現在年月日時刻
            );
    }
    public async Task PublishDevicePassiveOnTimeStatusAsync()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        await SendSensorStateAsync(serial, "passive", new
        {
            cumulative_normal = bRouteControllerService.Meter.定時積算電力量計測値_正方向計測値?.kWh,
            cumulative_reverse = bRouteControllerService.Meter.定時積算電力量計測値_逆方向計測値?.kWh,
            timestamp = bRouteControllerService.Meter.定時積算電力量計測値_逆方向計測値?.datetime,
        });
        logger.LogInformation("ステータス(積算-定時)通知 {n}W,{r}W,{time}",
            bRouteControllerService.Meter.定時積算電力量計測値_正方向計測値?.kWh,
            bRouteControllerService.Meter.定時積算電力量計測値_逆方向計測値?.kWh,
            bRouteControllerService.Meter.定時積算電力量計測値_逆方向計測値?.datetime
            );
    }
    private async Task SendSensorStateAsync(
        string serial, string subTopic, object payload)
    {
        await mqttService.PublishAsync($"homeassistant/sensor/{serial}/state/{subTopic}", payload, false);
    }
    #endregion

    private void SubscribeCommandTopic()
    {
        var serial = bRouteControllerService.Meter.製造番号!;
        mqttService.Subscribe($"homeassistant/button/{serial}/cmd", async (payload) =>
        {
            logger.LogInformation("コマンドを受信:{payload}", payload);
            if (payload == "active")
            {
                await bRouteControllerService.ReadActivePropertiesAsync();
            }
            else if (payload == "passive")
            {
                await bRouteControllerService.ReadPassivePropertiesAsync();
            }
        });
    }


}