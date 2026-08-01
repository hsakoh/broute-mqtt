namespace BRouteController;

public class BRouteOptions
{
    public string Id { get; set; } = default!;

    public string Pw { get; set; } = default!;

    public string SerialPort { get; set; } = default!;

    public bool UseBP35C0Commands { get; set; } = false;

    public bool ForcePANScan { get; set; } = false;
    public string PanDescSavePath { get; set; } = "/data/EPANDESC.json";

    public TimeSpan InstantaneousValueInterval { get; set; } = TimeSpan.FromMinutes(5);

    public int PanScanMaxRetryAttempts { get; set; } = 3;
    public TimeSpan PanScanRetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan PanaConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public int PanaConnectMaxRetryAttempts { get; set; } = 3;
    public TimeSpan PanaConnectRetryDelay { get; set; } = TimeSpan.FromSeconds(60);

    public TimeSpan PropertyReadTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public int PropertyReadMaxRetryAttempts { get; set; } = 2;
    public TimeSpan PropertyReadRetryDelay { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan PropertyReadIntervalDelay { get; set; } = TimeSpan.FromSeconds(2);

    public bool ContinuePollingOnError { get; set; } = true;

    /// <summary>
    /// broute-wifi-mqtt と同一メーターを同時に参照する場合に、
    /// MQTT Discovery の識別子(トピック/unique_id/device.identifiers)へ "_wisun" を付与して衝突を回避する
    /// </summary>
    public bool AddWiSunSuffix { get; set; } = false;
}
