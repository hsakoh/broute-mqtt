using EchoDotNetLite;
using EchoDotNetLite.Common;
using EchoDotNetLite.Models;
using EchoDotNetLiteSkstackIpBridge;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SkstackIpDotNet.Responses;
using System.IO.Ports;
using System.Text;

namespace BRouteController;

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

    public async Task InitalizeAsync(CancellationToken ct)
    {
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
    public async Task PollAsync(CancellationToken ct)
    {
        //Timer Loop
        var timer = new PeriodicTimer(_optionsMonitor.CurrentValue.InstantaneousValueInterval);
        while (await timer.WaitForNextTickAsync(ct))
        {
            await ReadActivePropertiesAsync(_optionsMonitor.CurrentValue.ContinuePollingOnError);
        }
    }

    public Func<Task>? PassivePropertiesReadedCallback;
    public Func<Task>? PassivePropertiesOnTimeCallback;
    public Func<Task>? ActivePropertiesReadedCallback;

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
        await Task.Delay(TimeSpan.FromSeconds(2));
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
                if(count != _optionsMonitor.CurrentValue.PanScanMaxRetryAttempts)
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
            if (count != _optionsMonitor.CurrentValue.PanScanMaxRetryAttempts)
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
            _logger.LogInformation("EchoProperty Change {Property} {HexValue}", echoPropertyInstance.GetDebugString(), SkstackIpDotNet.BytesConvert.ToHexString(e));

            if (Meter != null)
            {
                if (echoPropertyInstance.Spec.Code == 0xE0 || echoPropertyInstance.Spec.Code == 0xE3)
                {
                    //0xE0 積算電力量計測値 (正方向計測値)
                    //0xE3 積算電力量計測値 (逆方向計測値)
                    if (PassivePropertiesReadedCallback != null)
                    {
                        Task.Run(PassivePropertiesReadedCallback);
                    }
                }
                if (echoPropertyInstance.Spec.Code == 0xEA || echoPropertyInstance.Spec.Code == 0xEB)
                {
                    //0xEA 定時積算電力量計測値 (正方向計測値)
                    //0xEB 定時積算電力量計測値 (逆方向計測値)
                    if (PassivePropertiesOnTimeCallback != null)
                    {
                        Task.Run(PassivePropertiesOnTimeCallback);
                    }
                }
                if (echoPropertyInstance.Spec.Code == 0xE7 || echoPropertyInstance.Spec.Code == 0xE8)
                {
                    //0xE7 瞬時電力計測値
                    //0xE8 瞬時電流計測値
                    if (ActivePropertiesReadedCallback != null)
                    {
                        Task.Run(ActivePropertiesReadedCallback);
                    }
                }
            }
        }
    }
}