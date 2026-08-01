using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Models;
using EchoDotNetLiteSkstackIpBridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SkstackIpDotNet.Responses;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;

namespace BRouteController;

/// <summary>
/// ボタン押下等で要求された読み出しの種別(複数モードでは次回巡回時まで保留される)
/// </summary>
[Flags]
public enum PendingReadKind
{
    None = 0,
    Active = 1,
    Passive = 2,
    OneMin = 4,
}

public class BRouteControllerService : IDisposable
{
    private readonly ILogger<BRouteControllerService> _logger;
    private readonly IOptionsMonitor<BRouteOptions> _optionsMonitor;
    private readonly SkstackIpPANAClient _skStackClient;
    private readonly EchoClient _echoClient;
    private readonly SemaphoreSlim _semaphore;

    public BRouteControllerService(
        ILogger<BRouteControllerService> logger
        , IOptionsMonitor<BRouteOptions> optionsMonitor
        , SkstackIpPANAClient skStackClient
        , EchoClient echoClient)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _skStackClient = skStackClient;
        _echoClient = echoClient;
        _semaphore = new SemaphoreSlim(1, 1);

        _echoClient.OnNodeJoined += OnNodeJoined;

        //コントローラとしてふるまう
        _echoClient.SelfNode.Devices.Add(
            new EchoObjectInstance(
                EchoDotNetLite.Specifications.機器.管理操作関連機器.コントローラ, 0x01));

    }
    public void Dispose()
    {
        _logger.LogTrace("Dispose");
        _skStackClient?.Close();
        GC.SuppressFinalize(this);
    }
    public 低圧スマート電力量メータ Meter { get; private set; } = default!;

    /// <summary>単体/複数モード(InitalizeAsync で確定)</summary>
    public BRouteMode Mode { get; private set; } = BRouteMode.Single;

    /// <summary>複数モードで検出済みのメーター(キーは製造番号)</summary>
    public ConcurrentDictionary<string, 低圧スマート電力量メータ> Meters { get; } = new();

    /// <summary>複数モードの巡回状態(設定順)</summary>
    private List<MeterVisitState> _meterStates = [];

    /// <summary>ボタン押下等の保留読み出し(キーは製造番号)</summary>
    private readonly ConcurrentDictionary<string, PendingReadKind> _pendingReads = new();

    /// <summary>連続失敗したメーターの巡回を間引く最大サイクル数</summary>
    private const int MaxPollSkipCycles = 5;

    private sealed class MeterVisitState
    {
        public required BRouteMeterCredential Credential { get; init; }
        /// <summary>BルートID下8桁(=拡張ビーコンの Pairing ID)</summary>
        public required string PairId { get; init; }
        public required string PanCachePath { get; init; }
        public bool DiscoveryCompleted { get; set; }
        public string? Serial { get; set; }
        public int ConsecutiveFailures { get; set; }
        public int SkipRemaining { get; set; }
        /// <summary>PAN未発見のため、この起動中は巡回対象から除外(再試行は再起動時)</summary>
        public bool Excluded { get; set; }
    }

    /// <summary>PANスキャンをリトライしても対象のPANが見つからなかった</summary>
    private sealed class PanNotFoundException(string message) : ApplicationException(message);

    /// <summary>接続後のメーター初期化(プロパティマップ読み込み)がタイムアウトした</summary>
    private sealed class MeterInitializeTimeoutException(string message) : ApplicationException(message);

    public async Task InitalizeAsync(CancellationToken ct)
    {
        Mode = _optionsMonitor.CurrentValue.ResolveMode();
        if (Mode == BRouteMode.Multiple)
        {
            await InitalizeMultiAsync(ct);
            return;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        await _skStackClient.OpenAsync(_optionsMonitor.CurrentValue.SerialPort, 115200, 8, Parity.None, StopBits.One);

        //PANスキャン
        EPANDESC epandesc = await ScanPanAsync(cts.Token);
        //PANA接続シーケンス
        await ConnectPanaAsync(epandesc, cts.Token);
        //プロパティマップ読み込み
        await ReadAllPropertyMapAsync(cts.Token);
        //GET対応プロパティの値をすべて取得
        var (node, device) = await ReadAllPropertiesAsync(cts.Token);

        Meter = new 低圧スマート電力量メータ(node, device);
    }

    private async Task InitalizeMultiAsync(CancellationToken ct)
    {
        var options = _optionsMonitor.CurrentValue;
        var states = new List<MeterVisitState>();
        foreach (var (credential, index) in options.Meters.Select((m, i) => (m, i)))
        {
            if (string.IsNullOrEmpty(credential.Id) || string.IsNullOrEmpty(credential.Pw))
            {
                throw new ApplicationException($"BRoute:Meters[{index}] の Id/Pw が未設定です");
            }
            if (credential.Id.Length < 8)
            {
                throw new ApplicationException($"BRoute:Meters[{index}] の Id が短すぎます(下8桁を PairingID として使用します)");
            }
            var pairId = credential.Id[^8..];
            if (states.Any(s => s.PairId.Equals(pairId, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ApplicationException($"BRoute:Meters[{index}] の PairingID(ID下8桁:{pairId})が重複しています");
            }
            states.Add(new MeterVisitState
            {
                Credential = credential,
                PairId = pairId,
                PanCachePath = Path.Combine(
                    Path.GetDirectoryName(options.PanDescSavePath) ?? string.Empty,
                    $"EPANDESC.{pairId}.json"),
            });
        }
        _meterStates = states;

        await _skStackClient.OpenAsync(options.SerialPort, 115200, 8, Parity.None, StopBits.One);
        //自ノードのIPアドレスはセッションに依らないため1回だけ設定
        _echoClient.Initialize(_skStackClient.SelfIpaddr);
        _logger.LogInformation("複数モード: {Count}台のメーターを巡回します", _meterStates.Count);
        //各メーターへの接続・初期化は巡回(PollAsync)に委ねる
    }

    public async Task PollAsync(CancellationToken ct)
    {
        if (Mode == BRouteMode.Multiple)
        {
            await PollMultiAsync(ct);
            return;
        }
        //Timer Loop
        var timer = new PeriodicTimer(_optionsMonitor.CurrentValue.InstantaneousValueInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            if (!_skStackClient.IsPanaSessionAlive)
            {
                //EVENT 0x26/0x29 等でセッションが失われた場合の自動復旧
                try
                {
                    await ReconnectPanaAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PANAセッションの再接続に失敗");
                    if (!_optionsMonitor.CurrentValue.ContinuePollingOnError)
                    {
                        throw;
                    }
                    continue;
                }
            }
            await ReadActivePropertiesAsync(_optionsMonitor.CurrentValue.ContinuePollingOnError);
        }
    }

    /// <summary>
    /// 単体モードでセッション喪失から再接続する。
    /// キャッシュした PAN 情報で再接続を試み、失敗した場合はキャッシュを破棄して再スキャンする
    /// </summary>
    private async Task ReconnectPanaAsync(CancellationToken ct)
    {
        _logger.LogWarning("PANAセッションが失われているため再接続します");
        var options = _optionsMonitor.CurrentValue;
        await _skStackClient.SetIdPasswordAsync(options.Id, options.Pw);

        if (File.Exists(options.PanDescSavePath))
        {
            var cached = JsonConvert.DeserializeObject<EPANDESC>(await File.ReadAllTextAsync(options.PanDescSavePath, ct));
            if (cached != null
                && await _skStackClient.JoinAsync(cached, (int)options.PanaConnectTimeout.TotalMilliseconds))
            {
                _logger.LogInformation("PANAセッションを再確立しました");
                return;
            }
            //キャッシュで繋がらない場合は破棄して再スキャンから
            _logger.LogWarning("キャッシュしたPAN情報で再接続できないため再スキャンします");
            File.Delete(options.PanDescSavePath);
        }
        var epandesc = await ScanPanAsync(ct);
        await ConnectPanaAsync(epandesc, ct);
        _logger.LogInformation("PANAセッションを再確立しました");
    }

    private async Task PollMultiAsync(CancellationToken ct)
    {
        //InstantaneousValueInterval は巡回の開始間隔(1巡がこれを超えた場合は続けて次巡回)
        var timer = new PeriodicTimer(_optionsMonitor.CurrentValue.InstantaneousValueInterval);
        do
        {
            foreach (var state in _meterStates)
            {
                ct.ThrowIfCancellationRequested();
                if (state.Excluded)
                {
                    continue;
                }
                if (state.SkipRemaining > 0)
                {
                    state.SkipRemaining--;
                    _logger.LogInformation("PairingID:{PairId} は連続失敗のため今回の巡回をスキップ(残り{Remaining}回)", state.PairId, state.SkipRemaining);
                    continue;
                }
                try
                {
                    await VisitMeterAsync(state, ct);
                    state.ConsecutiveFailures = 0;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    //アプリ停止によるキャンセルのみ伝播する(メーター個別のタイムアウトは下の失敗処理に流す)
                    throw;
                }
                catch (Exception ex)
                {
                    state.ConsecutiveFailures++;
                    state.SkipRemaining = Math.Min(state.ConsecutiveFailures - 1, MaxPollSkipCycles);
                    _logger.LogError(ex, "PairingID:{PairId} の巡回で例外(連続{Failures}回目)", state.PairId, state.ConsecutiveFailures);
                    //一度も接続できないままPANが見つからないメーターは圏外とみなし、
                    //スキャンの繰り返しで巡回全体を停滞させないよう、この起動中は除外する
                    if (ex is PanNotFoundException && !state.DiscoveryCompleted)
                    {
                        state.Excluded = true;
                        _logger.LogWarning("PairingID:{PairId} はPANが見つからないため、この起動中は巡回対象から除外します(再試行するには再起動してください)", state.PairId);
                    }
                    if (!_optionsMonitor.CurrentValue.ContinuePollingOnError)
                    {
                        throw;
                    }
                }
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    /// <summary>
    /// 1メーターへの1訪問: 接続→(初回のみ初期化)→瞬時値/定時積算の取得→保留コマンド→切断
    /// </summary>
    private async Task VisitMeterAsync(MeterVisitState state, CancellationToken ct)
    {
        var options = _optionsMonitor.CurrentValue;
        _logger.LogInformation("PairingID:{PairId} の巡回を開始", state.PairId);
        //メーター毎に PSK/RBID が異なるため毎訪問設定する
        await _skStackClient.SetIdPasswordAsync(state.Credential.Id, state.Credential.Pw);

        var (epandesc, fromCache) = await ScanPanMultiAsync(state, ct);
        var joined = await _skStackClient.JoinAsync(epandesc, (int)options.PanaConnectTimeout.TotalMilliseconds);
        if (!joined && fromCache)
        {
            //キャッシュした PAN 情報が古い可能性があるため、破棄して再スキャン→再接続を1回だけ試す
            _logger.LogWarning("キャッシュしたPAN情報での接続に失敗。キャッシュを破棄して再スキャンします(PairingID:{PairId})", state.PairId);
            File.Delete(state.PanCachePath);
            (epandesc, _) = await ScanPanMultiAsync(state, ct);
            joined = await _skStackClient.JoinAsync(epandesc, (int)options.PanaConnectTimeout.TotalMilliseconds);
        }
        if (!joined)
        {
            throw new ApplicationException($"PANA接続に失敗(PairingID:{state.PairId})");
        }
        try
        {
            (EchoNode node, EchoObjectInstance device) initialized;
            try
            {
                initialized = await EnsureMeterInitializedAsync(state, ct);
            }
            catch (MeterInitializeTimeoutException)
            {
                //接続直後に ECHONET Lite の応答だけが得られないセッションになることがあるため、
                //セッションを張り直して同一訪問内でもう一度だけ初期化を試す
                _logger.LogWarning("初期化がタイムアウトしたため、セッションを再確立して再試行します(PairingID:{PairId})", state.PairId);
                await _skStackClient.TerminateAsync((int)options.SkTermTimeout.TotalMilliseconds);
                //SKTERM直後の再認証はメーター側のセッション後始末と競合して0x24になることがあるため、少し待ってから張り直す
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                if (!await _skStackClient.JoinAsync(epandesc, (int)options.PanaConnectTimeout.TotalMilliseconds))
                {
                    throw new ApplicationException($"PANA接続に失敗(PairingID:{state.PairId})");
                }
                initialized = await EnsureMeterInitializedAsync(state, ct);
            }
            var (node, device) = initialized;

            await _semaphore.WaitAsync(ct);
            try
            {
                //0x97 現在時刻設定 / 0x98 現在年月日設定
                await ReadTargetPropertiesAsync(node, device, [0x97, 0x98]);
                //0xE7 瞬時電力計測値 / 0xE8 瞬時電流計測値
                await ReadTargetPropertiesAsync(node, device, [0xE7, 0xE8]);
                //0xEA/0xEB 定時積算電力量計測値(30分毎の定時値。INF通知の代わりにポーリングで取得)
                await ReadTargetPropertiesAsync(node, device, [0xEA, 0xEB]);

                //ボタン押下等で保留された読み出しを実行(Active は上で取得済みのためフラグ消化のみ)
                if (state.Serial != null && _pendingReads.TryRemove(state.Serial, out var pending))
                {
                    if ((pending & PendingReadKind.Passive) != 0)
                    {
                        await ReadTargetPropertiesAsync(node, device, [0xE0, 0xE3]);
                    }
                    if ((pending & PendingReadKind.OneMin) != 0)
                    {
                        await ReadTargetPropertiesAsync(node, device, [0xD0]);
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
            _logger.LogInformation("PairingID:{PairId} の巡回が完了", state.PairId);
        }
        finally
        {
            await _skStackClient.TerminateAsync((int)options.SkTermTimeout.TotalMilliseconds);
        }
    }

    private async Task<(EPANDESC epandesc, bool fromCache)> ScanPanMultiAsync(MeterVisitState state, CancellationToken ct)
    {
        var options = _optionsMonitor.CurrentValue;
        if (!options.ForcePANScan && File.Exists(state.PanCachePath))
        {
            var cached = JsonConvert.DeserializeObject<EPANDESC>(await File.ReadAllTextAsync(state.PanCachePath, ct));
            if (cached != null)
            {
                _logger.LogInformation("PANスキャンスキップ(PairingID:{PairId})", state.PairId);
                return (cached, true);
            }
        }
        EPANDESC? epandesc = null;
        for (var count = 0; count <= options.PanScanMaxRetryAttempts; count++)
        {
            var (scanResult, found) = await _skStackClient.ScanAsync(state.PairId);
            if (scanResult)
            {
                epandesc = found;
                _logger.LogInformation("PANスキャン{count}(PairingID:{PairId})", count + 1, state.PairId);
                break;
            }
            ct.ThrowIfCancellationRequested();
            if (count != options.PanScanMaxRetryAttempts)
            {
                _logger.LogWarning("{Delay}後にスキャンを再試行します", options.PanScanRetryDelay);
                await Task.Delay(options.PanScanRetryDelay, ct);
            }
        }
        if (epandesc == null)
        {
            throw new PanNotFoundException($"PANスキャン リトライオーバー(PairingID:{state.PairId})");
        }
        Directory.CreateDirectory(Path.GetDirectoryName(state.PanCachePath)!);
        await File.WriteAllTextAsync(state.PanCachePath,
            JsonConvert.SerializeObject(epandesc, Formatting.Indented), Encoding.UTF8, ct);
        return (epandesc, false);
    }

    /// <summary>
    /// 現在接続中のメーターのノード/デバイスを解決する。
    /// 初回訪問(またはメーター交換等でノードが未知)の場合は、
    /// インスタンスリスト＋プロパティマップの取得と定性情報の読み出しを行い、Meters に登録する
    /// </summary>
    private async Task<(EchoNode node, EchoObjectInstance device)> EnsureMeterInitializedAsync(MeterVisitState state, CancellationToken ct)
    {
        var meterAddress = _skStackClient.SmartMaterIpaddr;
        var node = _echoClient.NodeList.FirstOrDefault(n => n.Address == meterAddress);
        var device = node?.Devices.FirstOrDefault(d => d.Spec == EchoDotNetLite.Specifications.機器.住宅設備関連機器.低圧スマート電力量メータ);

        if (device == null || !device.IsPropertyMapGet)
        {
            //不調なメーターが巡回全体を長時間塞がないよう、初期化待ちはこの時間で打ち切って次のメーターへ進む
            var discoveryTimeout = TimeSpan.FromSeconds(90);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(discoveryTimeout);
            try
            {
                await _echoClient.インスタンスリスト通知Async();
                await _echoClient.インスタンスリスト通知要求Async();
                _logger.LogInformation("プロパティマップ読み込み完了まで待機(PairingID:{PairId})", state.PairId);
                var waitCount = 0;
                while (true)
                {
                    node = _echoClient.NodeList.FirstOrDefault(n => n.Address == meterAddress);
                    device = node?.Devices.FirstOrDefault(d => d.Spec == EchoDotNetLite.Specifications.機器.住宅設備関連機器.低圧スマート電力量メータ);
                    if (device != null && device.IsPropertyMapGet)
                    {
                        break;
                    }
                    cts.Token.ThrowIfCancellationRequested();
                    _logger.LogInformation("プロパティマップ読み込み待機中");
                    await Task.Delay(2 * 1000, cts.Token);
                    waitCount++;
                    if (waitCount % 10 == 0)
                    {
                        //要求のロストやプロパティマップ読み取りのタイムアウトから回復するため再送する
                        _logger.LogWarning("応答がないためインスタンスリスト通知要求を再送します(PairingID:{PairId})", state.PairId);
                        await _echoClient.インスタンスリスト通知要求Async();
                    }
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new MeterInitializeTimeoutException($"プロパティマップ読み込みがタイムアウトしました(PairingID:{state.PairId})");
            }
        }

        if (!state.DiscoveryCompleted || state.Serial == null || !Meters.ContainsKey(state.Serial))
        {
            //初回訪問: 換算に必要な係数と定性情報を取得
            await _semaphore.WaitAsync(ct);
            try
            {
                //0xD3 係数 / 0xE1 積算電力量単位 / 0xD7 積算電力量有効桁数
                await ReadTargetPropertiesAsync(node!, device, [0xD3, 0xE1, 0xD7]);
                //0x8A メーカコード / 0x8D 製造番号 / 0x82 規格Version情報 / 0x81 設置場所
                await ReadTargetPropertiesAsync(node!, device, [0x8A, 0x8D, 0x82, 0x81]);
                //0xC0 Bルート識別番号(第2世代スマートメーターのみ)
                await ReadTargetPropertiesAsync(node!, device, [0xC0]);
            }
            finally
            {
                _semaphore.Release();
            }
            var meter = new 低圧スマート電力量メータ(node!, device);
            var serial = meter.製造番号
                ?? throw new ApplicationException($"製造番号(0x8D)が取得できませんでした(PairingID:{state.PairId})");
            state.Serial = serial;
            Meters[serial] = meter;
            state.DiscoveryCompleted = true;
            _logger.LogInformation("メーターを検出: 製造番号:{Serial} (PairingID:{PairId})", serial, state.PairId);
            if (MeterDiscoveredCallback != null)
            {
                await MeterDiscoveredCallback(meter);
            }
        }
        return (node!, device);
    }

    /// <summary>
    /// 対象EPCのうちメーターのGetプロパティマップに載っているものだけを読み出す(該当なしなら何もしない)
    /// </summary>
    private async Task ReadTargetPropertiesAsync(EchoNode node, EchoObjectInstance device, byte[] target)
    {
        var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
        if (!properties.Any())
        {
            return;
        }
        await ReadPropertyWithRetry(node, device, properties);
    }

    /// <summary>
    /// ボタン押下等による読み出し要求。
    /// 単体モードは即時実行、複数モードは次回巡回時まで保留する
    /// </summary>
    public async Task RequestReadAsync(低圧スマート電力量メータ meter, PendingReadKind kind)
    {
        if (Mode == BRouteMode.Single)
        {
            switch (kind)
            {
                case PendingReadKind.Active:
                    await ReadActivePropertiesAsync();
                    break;
                case PendingReadKind.Passive:
                    await ReadPassivePropertiesAsync();
                    break;
                case PendingReadKind.OneMin:
                    await ReadPassive1MinPropertiesAsync();
                    break;
                default:
                    break;
            }
            return;
        }
        var serial = meter.製造番号!;
        _pendingReads.AddOrUpdate(serial, kind, (_, current) => current | kind);
        _logger.LogInformation("製造番号:{Serial} への {Kind} 読み出しを次回巡回時に実行します", serial, kind);
    }

    public Func<低圧スマート電力量メータ, Task>? PassivePropertiesReadedCallback;
    public Func<低圧スマート電力量メータ, Task>? PassivePropertiesOnTimeCallback;
    public Func<低圧スマート電力量メータ, Task>? Passive1MinPropertiesReadedCallback;
    public Func<低圧スマート電力量メータ, Task>? ActivePropertiesReadedCallback;
    /// <summary>複数モードでメーターを初回検出したときに呼ばれる(discovery 公開の起点)</summary>
    public Func<低圧スマート電力量メータ, Task>? MeterDiscoveredCallback;

    public async Task ReadActivePropertiesAsync(bool continueOnError = false)
    {
        var node = Meter.EchoNode;
        var device = Meter.EchoObjectInstance;
        await _semaphore.WaitAsync();
        try
        {
            {
                //0x97 現在時刻設定
                //0x98 現在年月日設定
                //0xD3 係数
                //0xE1 積算電力量単位 （正方向、逆方向計測値）
                //0xD7 積算電力量有効桁数
                var target = new byte[] { 0x97, 0x98, 0xD3, 0xE1, 0xD7 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
            {
                //0xE7 瞬時電力計測値
                //0xE8 瞬時電流計測値
                var target = new byte[] { 0xE7, 0xE8 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロパティ値読み出しで例外");
            if (!continueOnError)
            {
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ReadPropertyWithRetry(EchoNode node, EchoObjectInstance device, IEnumerable<EchoPropertyInstance> properties)
    {
        (bool, List<PropertyRequest>)? readResult = null;
        for (var count = 0; count <= _optionsMonitor.CurrentValue.PropertyReadMaxRetryAttempts; count++)
        {
            try
            {
                readResult = await _echoClient.プロパティ値読み出し(
                _echoClient.SelfNode.Devices.First(),//コントローラー
                node, device, properties
                    , (int)_optionsMonitor.CurrentValue.PropertyReadTimeout.TotalMilliseconds);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Delay} 後にプロパティ値読み出しを再試行します", _optionsMonitor.CurrentValue.PropertyReadRetryDelay);
                await Task.Delay(_optionsMonitor.CurrentValue.PropertyReadRetryDelay);
            }
        }
        if (readResult == null)
        {
            _logger.LogWarning("プロパティ値読み出し リトライオーバー");
            throw new ApplicationException("プロパティ値読み出し リトライオーバー");
        }
        await Task.Delay(_optionsMonitor.CurrentValue.PropertyReadIntervalDelay);
    }

    public async Task ReadPassivePropertiesAsync()
    {
        var node = Meter.EchoNode;
        var device = Meter.EchoObjectInstance;
        await _semaphore.WaitAsync();
        try
        {
            {
                //0x97 現在時刻設定
                //0x98 現在年月日設定
                //0xD3 係数
                //0xE1 積算電力量単位 （正方向、逆方向計測値）
                //0xD7 積算電力量有効桁数
                var target = new byte[] { 0x97, 0x98, 0xD3, 0xE1, 0xD7 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
            {
                //0xE0 積算電力量計測値 (正方向計測値)
                //0xE3 積算電力量計測値 (逆方向計測値)
                var target = new byte[] { 0xE0, 0xE3 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロパティ値読み出しで例外");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ReadPassive1MinPropertiesAsync()
    {
        var node = Meter.EchoNode;
        var device = Meter.EchoObjectInstance;
        await _semaphore.WaitAsync();
        try
        {
            {
                //0x97 現在時刻設定
                //0x98 現在年月日設定
                //0xD3 係数
                //0xE1 積算電力量単位 （正方向、逆方向計測値）
                //0xD7 積算電力量有効桁数
                var target = new byte[] { 0x97, 0x98, 0xD3, 0xE1, 0xD7 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
            {
                //0xD0 1分積算電力量計測値（正方向、逆方向計測値）
                var target = new byte[] { 0xD0 };
                var properties = device.GETProperties.Where(p => target.Contains(p.Spec.Code));
                await ReadPropertyWithRetry(node, device, properties);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "プロパティ値読み出しで例外");
        }
        finally
        {
            _semaphore.Release();
        }
    }


    private async Task<EPANDESC> ScanPanAsync(CancellationToken ct)
    {
        EPANDESC? epandesc = null;
        if (!_optionsMonitor.CurrentValue.ForcePANScan
            && File.Exists(_optionsMonitor.CurrentValue.PanDescSavePath))
        {
            var pandesc = await File.ReadAllTextAsync(_optionsMonitor.CurrentValue.PanDescSavePath, ct);
            epandesc = JsonConvert.DeserializeObject<EPANDESC>(pandesc);
            _logger.LogInformation("PANスキャンスキップ");
        }
        await _skStackClient.SetIdPasswordAsync(_optionsMonitor.CurrentValue.Id, _optionsMonitor.CurrentValue.Pw);

        if (epandesc == null)
        {
            for (var count = 0; count <= _optionsMonitor.CurrentValue.PanScanMaxRetryAttempts; count++)
            {
                (var scanResult, epandesc) = await _skStackClient.ScanAsync();
                if (scanResult)
                {
                    _logger.LogInformation("PANスキャン{count}", count + 1);
                    break;
                }
                ct.ThrowIfCancellationRequested();
                if (count != _optionsMonitor.CurrentValue.PanScanMaxRetryAttempts)
                {
                    _logger.LogWarning("{Delay}後にスキャンを再試行します", _optionsMonitor.CurrentValue.PanScanRetryDelay);
                    await Task.Delay(_optionsMonitor.CurrentValue.PanScanRetryDelay, ct);
                }
            }
            if (epandesc == null)
            {
                _logger.LogWarning("PANスキャン リトライオーバー");
                throw new ApplicationException("PANスキャン リトライオーバー");
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(_optionsMonitor.CurrentValue.PanDescSavePath)!);
        await File.WriteAllTextAsync(_optionsMonitor.CurrentValue.PanDescSavePath,
            JsonConvert.SerializeObject(epandesc, Formatting.Indented), Encoding.UTF8, ct);
        return epandesc;
    }

    private async Task ConnectPanaAsync(EPANDESC epandesc, CancellationToken ct)
    {
        bool isSuccess = false;
        for (var count = 0; count <= _optionsMonitor.CurrentValue.PanaConnectMaxRetryAttempts; count++)
        {
            isSuccess = await _skStackClient.JoinAsync(epandesc, (int)_optionsMonitor.CurrentValue.PanaConnectTimeout.TotalMilliseconds);
            ct.ThrowIfCancellationRequested();
            if (isSuccess)
            {
                break;
            }
            if (count != _optionsMonitor.CurrentValue.PanaConnectMaxRetryAttempts)
            {
                _logger.LogWarning("{Delay}後に接続を再試行します", _optionsMonitor.CurrentValue.PanaConnectRetryDelay);
                await Task.Delay(_optionsMonitor.CurrentValue.PanaConnectRetryDelay, ct);
            }
        }
        if (!isSuccess)
        {
            _logger.LogWarning("PANA接続シーケンス リトライオーバー");
            throw new ApplicationException("PANA接続シーケンス リトライオーバー");
        }
    }

    private async Task ReadAllPropertyMapAsync(CancellationToken ct)
    {
        _echoClient.Initialize(_skStackClient.SelfIpaddr);
        await _echoClient.インスタンスリスト通知Async();
        await _echoClient.インスタンスリスト通知要求Async();

        _logger.LogInformation("プロパティマップ読み込み完了まで待機");
        while (_echoClient.NodeList?.FirstOrDefault()?.Devices?.FirstOrDefault() == null
                || !_echoClient.NodeList.First().Devices.First().IsPropertyMapGet)
        {
            ct.ThrowIfCancellationRequested();
            _logger.LogInformation("プロパティマップ読み込み待機中");
            await Task.Delay(2 * 1000, ct);
        }
    }

#pragma warning disable IDE0060 // 未使用のパラメーターを削除します
    private async Task<(EchoNode node, EchoObjectInstance device)> ReadAllPropertiesAsync(CancellationToken cs)
#pragma warning restore IDE0060 // 未使用のパラメーターを削除します
    {
        //Bルートなので、低圧スマート電力量メータ以外のデバイスは存在しない前提
        var node = _echoClient.NodeList.First();
        var device = node.Devices.First();

        _logger.LogDebug("低圧スマート電力量メータ デバイスのGET対応プロパティの値をすべて取得");
        //まとめてもできるけど、大量に指定するとこけるのでプロパティ毎に
        foreach (var prop in device.GETProperties)
        {
            await ReadPropertyWithRetry(node, device, [prop]);
        }
        return (node, device);
    }



    private void OnNodeJoined(object? sender, EchoDotNetLite.Models.EchoNode e)
    {
        _logger.LogInformation("EchoNode Add {Address}", e.Address);
        e.OnCollectionChanged += OnEchoObjectChange;
    }

    private void OnEchoObjectChange(object? sender, (CollectionChangeType type, EchoObjectInstance instance) e)
    {
        switch (e.type)
        {
            case CollectionChangeType.Add:
                _logger.LogInformation("EchoObject Add {Object}", e.instance.GetDebugString());
                e.instance.OnCollectionChanged += OnEchoPropertyChange;
                break;
            case CollectionChangeType.Remove:
                _logger.LogInformation("EchoObject Remove {Object}", e.instance.GetDebugString());
                break;
            default:
                break;
        }
    }

    private void OnEchoPropertyChange(object? sender, (CollectionChangeType type, EchoPropertyInstance instance) e)
    {
        switch (e.type)
        {
            case CollectionChangeType.Add:
                _logger.LogInformation("EchoProperty Add {Property}", e.instance.GetDebugString());
                e.instance.ValueChanged += OnEchoPropertyValueChanged;
                break;
            case CollectionChangeType.Remove:
                _logger.LogInformation("EchoProperty Remove {Property}", e.instance.GetDebugString());
                break;
            default:
                break;
        }
    }

    private void OnEchoPropertyValueChanged(object? sender, byte[] e)
    {
        if (sender is EchoPropertyInstance echoPropertyInstance)
        {
            _logger.LogDebug("EchoProperty Change {Property} {HexValue}", echoPropertyInstance.GetDebugString(), SkstackIpDotNet.BytesConvert.ToHexString(e));

            //どのメーターのプロパティ変更かを特定
            低圧スマート電力量メータ? meter = Mode == BRouteMode.Single
                ? Meter
                : Meters.Values.FirstOrDefault(m => m.EchoObjectInstance.Properties.Contains(echoPropertyInstance));

            if (meter != null)
            {
                if (echoPropertyInstance.Spec.Code == 0xE0 || echoPropertyInstance.Spec.Code == 0xE3)
                {
                    //0xE0 積算電力量計測値 (正方向計測値)
                    //0xE3 積算電力量計測値 (逆方向計測値)
                    if (PassivePropertiesReadedCallback != null)
                    {
                        Task.Run(() => PassivePropertiesReadedCallback(meter));
                    }
                }
                if (echoPropertyInstance.Spec.Code == 0xEA || echoPropertyInstance.Spec.Code == 0xEB)
                {
                    //0xEA 定時積算電力量計測値 (正方向計測値)
                    //0xEB 定時積算電力量計測値 (逆方向計測値)
                    if (PassivePropertiesOnTimeCallback != null)
                    {
                        Task.Run(() => PassivePropertiesOnTimeCallback(meter));
                    }
                }
                if (echoPropertyInstance.Spec.Code == 0xD0)
                {
                    //0xD0 1分積算電力量計測値（正方向、逆方向計測値）
                    if (Passive1MinPropertiesReadedCallback != null)
                    {
                        Task.Run(() => Passive1MinPropertiesReadedCallback(meter));
                    }
                }
                if (echoPropertyInstance.Spec.Code == 0xE7 || echoPropertyInstance.Spec.Code == 0xE8)
                {
                    //0xE7 瞬時電力計測値
                    //0xE8 瞬時電流計測値
                    if (ActivePropertiesReadedCallback != null)
                    {
                        Task.Run(() => ActivePropertiesReadedCallback(meter));
                    }
                }
            }
        }
    }
}