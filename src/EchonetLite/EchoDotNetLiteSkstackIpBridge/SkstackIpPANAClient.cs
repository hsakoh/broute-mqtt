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
        private readonly SKDevice SKDevice;
        public SkstackIpPANAClient(ILogger<SkstackIpPANAClient> logger, SKDevice skDevice)
        {
            _logger = logger;
            SKDevice = skDevice;
            SKDevice.OnERXUDPReceived += ReceivedERXUDP;
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

        public async Task<(bool result, EPANDESC)> ScanAsync()
        {
            _logger.LogInformation($"スキャン開始");
            SkstackIpDotNet.Responses.EPANDESC pan = null;
            for (byte duration = 4; duration < 8; duration++)
            {
                _logger.LogDebug("スキャン時間:{duration}", duration);
                var scanResult = await SKDevice.SKScanActiveExAsync(0xFFFFFFFF, duration);

                if (scanResult.Any())
                {
                    pan = scanResult.First();
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
