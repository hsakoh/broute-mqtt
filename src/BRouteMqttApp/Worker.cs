using BRouteController;
using HomeAssistantAddOn.Mqtt;
using Microsoft.Extensions.Options;

namespace BRouteMqttApp;

public class Worker(
    ILogger<Worker> logger
        , BRouteControllerService bRouteControllerService
        , MqttService mqttService
        , IOptionsMonitor<BRouteOptions> bRouteOptions
        ) : BackgroundService
{
    /// <summary>
    /// broute-wifi-mqtt との同時稼働時に識別子の衝突を避けるためのサフィックス。
    /// トピック/unique_id/device.identifiers に付与する(センサー値としての製造番号には付与しない)
    /// </summary>
    private string GetSerial(低圧スマート電力量メータ meter)
        => meter.製造番号! + (bRouteOptions.CurrentValue.AddWiSunSuffix ? "_wisun" : "");

    private string DeviceName
        => nameof(低圧スマート電力量メータ) + (bRouteOptions.CurrentValue.AddWiSunSuffix ? "(Wi-SUN)" : "");

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await mqttService.StartAsync();
        await bRouteControllerService.InitalizeAsync(cancellationToken);

        if (bRouteControllerService.Mode == BRouteMode.Single)
        {
            var meter = bRouteControllerService.Meter;
            await PublishDeviceConfigsAsync(meter);

            await Task.Delay(5 * 1000, cancellationToken);

            await PublishDeviceActiveStatusAsync(meter);
            await PublishDevicePassiveStatusAsync(meter);
            if (MeterHasEpc(meter, 0xD0))
            {
                await PublishDevicePassive1MinStatusAsync(meter);
            }
            await PublishDeviceStaticStatusAsync(meter);
            SubscribeCommandTopic(meter);
        }

        bRouteControllerService.ActivePropertiesReadedCallback = PublishDeviceActiveStatusAsync;
        bRouteControllerService.PassivePropertiesReadedCallback = PublishDevicePassiveStatusAsync;
        bRouteControllerService.PassivePropertiesOnTimeCallback = PublishDevicePassiveOnTimeStatusAsync;
        bRouteControllerService.Passive1MinPropertiesReadedCallback = PublishDevicePassive1MinStatusAsync;
        //複数モード: 巡回でメーターを初回検出したときに discovery を公開する
        bRouteControllerService.MeterDiscoveredCallback = OnMeterDiscoveredAsync;

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

    private async Task OnMeterDiscoveredAsync(低圧スマート電力量メータ meter)
    {
        await PublishDeviceConfigsAsync(meter);
        //HA が discovery を処理するのを待ってから状態を送る(単体モードの起動シーケンスと同じ5秒)
        await Task.Delay(5 * 1000);
        await PublishDeviceStaticStatusAsync(meter);
        SubscribeCommandTopic(meter);
        //瞬時値・積算値は直後の巡回内の読み出しでコールバック経由で publish される
    }

    #region Configure Senser
    private async Task PublishDeviceConfigsAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await PublishSensorConfigAsync(serial, "placement", "設置場所", "static", icon: "mdi:map-marker");
        await PublishSensorConfigAsync(serial, "version", "規格Version情報", "static", icon: "mdi:information");
        await PublishSensorConfigAsync(serial, "makercode", "メーカコード", "static", icon: "mdi:factory");
        await PublishSensorConfigAsync(serial, "serialnumber", "製造番号", "static", icon: "mdi:identifier");
        if (MeterHasEpc(meter, 0xC0))
        {
            //0xC0 Bルート識別番号(第2世代スマートメーターのみ)
            await PublishSensorConfigAsync(serial, "b_route_id", "Bルート識別番号", "static", icon: "mdi:identifier");
        }
        if (MeterHasEpc(meter, 0xD0))
        {
            //0xD0 1分積算電力量計測値(第2世代スマートメーターのみ)
            await PublishSensorConfigAsync(serial, "cumulative_1min_normal", "1分積算電力量計測値(正方向)", "passive_1min"
                , device_class: "energy", state_class: "total_increasing", unit_of_measurement: "kWh");
            await PublishSensorConfigAsync(serial, "cumulative_1min_reverse", "1分積算電力量計測値(逆方向)", "passive_1min"
                , device_class: "energy", state_class: "total_increasing", unit_of_measurement: "kWh");
            await PublishSensorConfigAsync(serial, "passive_1min_timestamp", "更新日時(1分積算電力量)", "passive_1min"
                , device_class: "timestamp", value_template: "{% set ts = value_json.get('timestamp', {})  %} {% if ts %}\n  {{ (ts / 1000) | timestamp_local | as_datetime }}\n{% else %}\n  {{ this.state }}\n{% endif %}");
        }

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
            , device_class: "power", state_class: "measurement", unit_of_measurement: "W");
        await PublishSensorConfigAsync(serial, "active_timestamp", "更新日時(瞬時値)", "active"
            , device_class: "timestamp", value_template: "{% set ts = value_json.get('timestamp', {})  %} {% if ts %}\n  {{ (ts / 1000) | timestamp_local | as_datetime }}\n{% else %}\n  {{ this.state }}\n{% endif %}");

        await SendButtonConfigAsync(serial, "active", "瞬時値の取得", "update");
        await SendButtonConfigAsync(serial, "passive", "積算電力量の取得", "update");
        if (MeterHasEpc(meter, 0xD0))
        {
            await SendButtonConfigAsync(serial, "1min", "1分積算電力量の取得", "update");
        }

    }

    private static bool MeterHasEpc(低圧スマート電力量メータ meter, byte code)
        => meter.EchoObjectInstance.GETProperties.Any(p => p.Spec.Code == code);

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
            default_entity_id = $"sensor.{type}_{serial}",
            device = new
            {
                identifiers = new[] { $"smart_meter_{serial}" },
                name = DeviceName,
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
            default_entity_id = $"button.{type}_{serial}",
            device = new
            {
                identifiers = new[] { $"smart_meter_{serial}" },
                name = DeviceName,
            },
        };
        await mqttService.PublishAsync($"homeassistant/button/btn_{type}_{serial}/config", payload, true);
    }
    #endregion

    #region Notifiy Senser Stauts

    public async Task PublishDeviceStaticStatusAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await SendSensorStateAsync(serial, "static", new
        {
            placement = meter.設置場所,
            version = meter.規格Version情報,
            makercode = meter.メーカコード,
            serialnumber = meter.製造番号,
            b_route_id = meter.Bルート識別番号,
        });
        logger.LogInformation("ステータス(静的)通知 {a},{b},{c},{d},{e}",
            meter.設置場所,
            meter.規格Version情報,
            meter.メーカコード,
            meter.製造番号,
            meter.Bルート識別番号
            );
    }
    public async Task PublishDeviceActiveStatusAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await SendSensorStateAsync(serial, "active", new
        {
            instantaneous_current_r = meter.瞬時電流計測値?.r,
            instantaneous_current_t = meter.瞬時電流計測値?.t,
            instantaneous_electric_power = meter.瞬時電力計測値,
            timestamp = meter.現在年月日時刻
        });
        logger.LogInformation("ステータス(瞬時)通知 {serial} {r}A,{t}A,{e}W,{time}",
            meter.製造番号,
            meter.瞬時電流計測値?.r,
            meter.瞬時電流計測値?.t,
            meter.瞬時電力計測値,
            meter.現在年月日時刻
            );
    }
    public async Task PublishDevicePassiveStatusAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await SendSensorStateAsync(serial, "passive", new
        {
            cumulative_normal = meter.積算電力量計測値_正方向計測値,
            cumulative_reverse = meter.積算電力量計測値_逆方向計測値,
            timestamp = meter.現在年月日時刻
        });
        logger.LogInformation("ステータス(積算)通知 {serial} {n}W,{r}W,{time}",
            meter.製造番号,
            meter.積算電力量計測値_正方向計測値,
            meter.積算電力量計測値_逆方向計測値,
            meter.現在年月日時刻
            );
    }
    public async Task PublishDevicePassive1MinStatusAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await SendSensorStateAsync(serial, "passive_1min", new
        {
            cumulative_1min_normal = meter.一分積算電力量計測値?.normalKWh,
            cumulative_1min_reverse = meter.一分積算電力量計測値?.reverseKWh,
            timestamp = meter.一分積算電力量計測値?.datetime,
        });
        logger.LogInformation("ステータス(積算-1分)通知 {serial} {n}kWh,{r}kWh,{time}",
            meter.製造番号,
            meter.一分積算電力量計測値?.normalKWh,
            meter.一分積算電力量計測値?.reverseKWh,
            meter.一分積算電力量計測値?.datetime
            );
    }
    public async Task PublishDevicePassiveOnTimeStatusAsync(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        await SendSensorStateAsync(serial, "passive", new
        {
            cumulative_normal = meter.定時積算電力量計測値_正方向計測値?.kWh,
            cumulative_reverse = meter.定時積算電力量計測値_逆方向計測値?.kWh,
            timestamp = meter.定時積算電力量計測値_逆方向計測値?.datetime,
        });
        logger.LogInformation("ステータス(積算-定時)通知 {serial} {n}W,{r}W,{time}",
            meter.製造番号,
            meter.定時積算電力量計測値_正方向計測値?.kWh,
            meter.定時積算電力量計測値_逆方向計測値?.kWh,
            meter.定時積算電力量計測値_逆方向計測値?.datetime
            );
    }
    private async Task SendSensorStateAsync(
        string serial, string subTopic, object payload)
    {
        await mqttService.PublishAsync($"homeassistant/sensor/{serial}/state/{subTopic}", payload, false);
    }
    #endregion

    private void SubscribeCommandTopic(低圧スマート電力量メータ meter)
    {
        var serial = GetSerial(meter);
        mqttService.Subscribe($"homeassistant/button/{serial}/cmd", async (payload) =>
        {
            logger.LogInformation("コマンドを受信:{serial} {payload}", meter.製造番号, payload);
            if (payload == "active")
            {
                await bRouteControllerService.RequestReadAsync(meter, PendingReadKind.Active);
            }
            else if (payload == "passive")
            {
                await bRouteControllerService.RequestReadAsync(meter, PendingReadKind.Passive);
            }
            else if (payload == "1min")
            {
                await bRouteControllerService.RequestReadAsync(meter, PendingReadKind.OneMin);
            }
        });
    }


}
