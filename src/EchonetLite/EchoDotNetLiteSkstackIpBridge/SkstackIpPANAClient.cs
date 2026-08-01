using EchoDotNetLite;
using Microsoft.Extensions.Logging;
using SkstackIpDotNet;
using SkstackIpDotNet.Events;
using SkstackIpDotNet.Responses;
using System;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;

namespace EchoDotNetLiteSkstackIpBridge
{
    public class SkstackIpPANAClient : IPANAClient, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ISKDevice SKDevice;
        public SkstackIpPANAClient(ILogger<SkstackIpPANAClient> logger, ISKDevice skDevice)
        {
            _logger = logger;
            SKDevice = skDevice;
            SKDevice.OnERXUDPReceived += ReceivedERXUDP;
            SKDevice.OnEVENTReceived += OnPanaSessionEvent;
        }

        /// <summary>
        /// PANA セッションが確立しているか(EVENT 0x25 で true、0x26/0x27/0x28/0x29 で false)
        /// </summary>
        public bool IsPanaSessionAlive { get; private set; }

        /// <summary>
        /// PANA セッションが失われた(0x26/0x27/0x28/0x29)ときにイベント番号を通知する
        /// </summary>
        public event EventHandler<string> OnPanaSessionLost;

        private void OnPanaSessionEvent(object sendor, EVENT e)
        {
            switch (e.Num)
            {
                case "25":
                    IsPanaSessionAlive = true;
                    break;
                case "26":
                    _logger.LogWarning("0x26:相手から PANA セッションの終了を要求された");
                    IsPanaSessionAlive = false;
                    OnPanaSessionLost?.Invoke(this, e.Num);
                    break;
                case "27":
                    _logger.LogInformation("0x27:PANA セッションの終了に成功した");
                    IsPanaSessionAlive = false;
                    OnPanaSessionLost?.Invoke(this, e.Num);
                    break;
                case "28":
                    _logger.LogInformation("0x28:PANA セッションの終了要求に対する応答がなくタイムアウトした(セッションは終了扱い)");
                    IsPanaSessionAlive = false;
                    OnPanaSessionLost?.Invoke(this, e.Num);
                    break;
                case "29":
                    _logger.LogWarning("0x29:PANA セッションのライフタイムが経過して期限切れになった");
                    IsPanaSessionAlive = false;
                    OnPanaSessionLost?.Invoke(this, e.Num);
                    break;
                default:
                    break;
            }
        }

        public async Task OpenAsync(string port, int baud, int data, Parity parity, StopBits stopbits)
        {
            _logger.LogInformation("Open");
            _logger.LogDebug("Bridge Open port:{port},baud:{baud},data:{data},parity:{parity},stopbits:{stopbits}", port, baud, data, parity, stopbits);
            SKDevice.Open(port, baud, data, parity, stopbits);
            var info = await SKDevice.SKInfoAsync();
            SelfIpaddr = info.IPAddress;
        }

        public string BroadcastIpaddr { get; set; }

        public string SmartMaterIpaddr { get; set; }
        public string SelfIpaddr { get; set; }

        public async Task<bool> ScanAndJoinAsync(string bRouteId, string bRoutePassword)
        {
            await SetIdPasswordAsync(bRouteId, bRoutePassword);
            var (result, epandesc) = await ScanAsync();
            if (!result)
            {
                return false;
            }
            return await JoinAsync(epandesc);
        }
        public async Task SetIdPasswordAsync(string bRouteId, string bRoutePassword)
        {
            _logger.LogInformation($"ID、パスワードの設定");
            _logger.LogDebug($"パスワードの設定");
            await SKDevice.SKSetPwdAsync("C", bRoutePassword);
            _logger.LogDebug($"IDの設定");
            await SKDevice.SKSetRBIDAsync(bRouteId);
        }

        public async Task<(bool result, EPANDESC)> ScanAsync(string expectedPairId = null)
        {
            _logger.LogInformation($"スキャン開始");
            SkstackIpDotNet.Responses.EPANDESC pan = null;
            for (byte duration = 4; duration < 8; duration++)
            {
                _logger.LogDebug("スキャン時間:{duration}", duration);
                var scanResult = await SKDevice.SKScanActiveExAsync(0xFFFFFFFF, duration);

                //PairingID(BルートID下8桁)の指定がある場合、一致するPANのみ採用する
                var candidates = expectedPairId == null
                    ? scanResult
                    : scanResult.Where(p => string.Equals(p.PairID, expectedPairId, StringComparison.OrdinalIgnoreCase));
                foreach (var mismatch in scanResult.Except(candidates))
                {
                    _logger.LogDebug("PairingID不一致のPANを無視: PairingID:{PairID},PAN ID:{PanID}", mismatch.PairID, mismatch.PanID);
                }

                if (candidates.Any())
                {
                    pan = candidates.OrderByDescending(p => Convert.ToInt32(p.LQI ?? "0", 16)).First();
                    _logger.LogInformation("PAN発見: 論理チャンネル番号:{Channel},チャンネルページ:{ChannelPage},PAN ID:{PanID},アドレス:{Addr},RSSI:{LQI},PairingID:{PairID}", pan.Channel, pan.ChannelPage, pan.PanID, pan.Addr, pan.LQI, pan.PairID);
                    break;
                }
            }
            if (pan == null)
            {
                _logger.LogDebug($"PANが見つからない");
                return (false, null);
            }
            return (true, pan);
        }

        public async Task<bool> JoinAsync(EPANDESC epandesc, int timeoutMilliseconds = 30 * 1000)
        {
            await SKDevice.SKSRegAsync("S2", epandesc.Channel);
            await SKDevice.SKSRegAsync("S3", epandesc.PanID);
            var skll64 = await SKDevice.SKLl64Async(epandesc.Addr);
            //TODO Bルートの一斉同報の宛先ってスマートメーターだけ…?
            BroadcastIpaddr = skll64.Ipaddr;
            SmartMaterIpaddr = skll64.Ipaddr;
            var joinTCS = new TaskCompletionSource<bool>();
            var joinEvent = default(EventHandler<EVENT>);
            joinEvent += (sender, e) =>
            {
                if (e.Num == "24")
                {
                    _logger.LogWarning($"0x24:PANA による接続過程でエラーが発生した（接続が完了しなかった）");
                    joinTCS.SetResult(false);
                    SKDevice.OnEVENTReceived -= joinEvent;
                }
                if (e.Num == "25")
                {
                    _logger.LogInformation($"0x25:PANA による接続が完了した");
                    joinTCS.SetResult(true);
                    SKDevice.OnEVENTReceived -= joinEvent;
                }
            };
            SKDevice.OnEVENTReceived += joinEvent;
            _logger.LogInformation($"PANA接続シーケンス開始");
            await SKDevice.SKJoinAsync(SmartMaterIpaddr);
            if (await Task.WhenAny(joinTCS.Task, Task.Delay(timeoutMilliseconds)) == joinTCS.Task)
            {
                return await joinTCS.Task;
            }
            else
            {
                _logger.LogWarning($"PANA接続シーケンス タイムアウト");
                SKDevice.OnEVENTReceived -= joinEvent;
                return false;
            }
        }

        /// <summary>
        /// 現在の PANA セッションを SKTERM で終了する。
        /// 未接続なら何もしない。FAIL(ER10=未接続)・EVENT 0x28(相手無応答)・タイムアウトは
        /// いずれも「切断済み」とみなして true を返す(次の SKJOIN で新しいセッションを確立できる)。
        /// </summary>
        public async Task<bool> TerminateAsync(int timeoutMilliseconds = 10 * 1000)
        {
            if (!IsPanaSessionAlive)
            {
                _logger.LogDebug("PANAセッション未確立のため切断をスキップ");
                return true;
            }
            var termTCS = new TaskCompletionSource<bool>();
            var termEvent = default(EventHandler<EVENT>);
            termEvent += (sender, e) =>
            {
                if (e.Num == "27" || e.Num == "28")
                {
                    termTCS.TrySetResult(true);
                    SKDevice.OnEVENTReceived -= termEvent;
                }
            };
            SKDevice.OnEVENTReceived += termEvent;
            _logger.LogInformation($"PANAセッション切断シーケンス開始");
            var result = await SKDevice.SKTermAsync();
            if (result is FAIL)
            {
                //ER10: 接続が確立していない状態
                _logger.LogInformation("SKTERM が FAIL 応答(セッション未確立とみなす)");
                SKDevice.OnEVENTReceived -= termEvent;
                IsPanaSessionAlive = false;
                return true;
            }
            if (await Task.WhenAny(termTCS.Task, Task.Delay(timeoutMilliseconds)) == termTCS.Task)
            {
                return await termTCS.Task;
            }
            _logger.LogWarning($"PANAセッション切断シーケンス タイムアウト(切断済みとみなして続行)");
            SKDevice.OnEVENTReceived -= termEvent;
            IsPanaSessionAlive = false;
            return true;
        }

        public event EventHandler<(string, byte[])> OnEventReceived;

        public void ReceivedERXUDP(object sendor, ERXUDP erxudp)
        {
            OnEventReceived?.Invoke(this, (erxudp.Sender, BytesConvert.FromHexString(erxudp.Data)));
        }


        public async Task RequestAsync(string address, byte[] request)
        {
            address ??= BroadcastIpaddr;
            await SKDevice.SKSendToAsync(
                "1",
                address,
                "0E1A",
                SKSendToSec.SecOrNotTransfer,
                request);
        }


        public void Close()
        {
            if (SKDevice != null)
            {
                _logger.LogInformation("Close");
                SKDevice.Close();
            }
        }
        public void Dispose()
        {
            _logger.LogTrace("Dispose");
            SKDevice?.Close();
            SKDevice?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
