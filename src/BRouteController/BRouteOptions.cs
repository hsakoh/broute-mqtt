namespace BRouteController;

public class BRouteOptions
{
    public string Id { get; set; } = default!;

    public string Pw { get; set; } = default!;

    /// <summary>
    /// 複数モードで巡回するメーターの認証情報リスト。
    /// Id/Pw(単体モード)と排他で、両方設定されている場合は Id/Pw(単体モード)を優先する
    /// </summary>
    public List<BRouteMeterCredential> Meters { get; set; } = [];

    /// <summary>
    /// Id/Pw に値があれば単体モード、無ければ Meters の複数モード。どちらも無ければ設定エラー
    /// </summary>
    public BRouteMode ResolveMode()
    {
        if (!string.IsNullOrEmpty(Id) || !string.IsNullOrEmpty(Pw))
        {
            return BRouteMode.Single;
        }
        if (Meters.Count > 0)
        {
            return BRouteMode.Multiple;
        }
        throw new ApplicationException("BルートIDが未設定です。BRoute:Id/Pw(単体モード)または BRoute:Meters(複数モード)を設定してください");
    }

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

    /// <summary>
    /// SKTERM 後の PANA セッション終了イベント(0x27/0x28)待ちタイムアウト(複数モード)
    /// </summary>
    public TimeSpan SkTermTimeout { get; set; } = TimeSpan.FromSeconds(10);

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

public class BRouteMeterCredential
{
    public string Id { get; set; } = default!;
    public string Pw { get; set; } = default!;
}

public enum BRouteMode
{
    /// <summary>単一メーターと常時接続し、INF通知を受信する(従来動作)</summary>
    Single,
    /// <summary>複数メーターを 接続→取得→切断 で巡回し、通知に依存しない</summary>
    Multiple,
}
