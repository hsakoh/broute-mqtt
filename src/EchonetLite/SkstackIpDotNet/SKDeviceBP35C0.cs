using Microsoft.Extensions.Logging;
using SkstackIpDotNet.Commands;
using SkstackIpDotNet.Commands.BP35C0;
using SkstackIpDotNet.Events;
using SkstackIpDotNet.Responses;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkstackIpDotNet
{
    /// <summary>
    /// SKSTACK-IP (Single-hop Edition)
    /// SK コマンドのデバイス
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="logger">logger</param>
    public class SKDeviceBP35C0(ILogger<SKDeviceBP35C0> logger) : IDisposable, ISKDevice
    {
        private readonly ILogger _logger = logger;
        private SerialPort serialPort;

        /// <summary>
        /// 接続を開きます
        /// </summary>
        /// <param name="port">The name of the COM port, such as "COM1" or "COM33".</param>
        /// <param name="baud">The baud rate that is passed to the underlying driver.</param>
        /// <param name="data">
        /// The number of data bits. This is checked that the driver supports the data bits
        /// provided. The special type 16X is not supported.
        /// </param>
        /// <param name="parity">The parity for the data stream.</param>
        /// <param name="stopbits">Number of stop bits.</param>
        public void Open(string port, int baud, int data, Parity parity, StopBits stopbits)
        {
            _logger.LogInformation("Open");
            serialPort = new SerialPort(port, baud, parity, data, stopbits);
            serialPort.DataReceived += DataReceived;
            serialPort.Open();
        }

        /// <summary>
        /// 接続を閉じます
        /// </summary>
        public void Close()
        {
            if (serialPort?.IsOpen ?? false)
            {
                _logger.LogInformation("Close");
                serialPort.Close();
            }
        }
        /// <summary>
        /// デバイスを破棄します
        /// </summary>
        public void Dispose()
        {
            _logger.LogTrace("Dispose");
            if (serialPort is not null)
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                }

                serialPort.Dispose();

                serialPort = null;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>受信途中(前回CRLFで終わっていない行)のバッファ</summary>
        private string receiveBuffer = null;

        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var buffer = new byte[serialPort.BytesToRead];
            //シリアルポート
            serialPort.Read(buffer, 0, buffer.Length);

            string value = Encoding.ASCII.GetString(buffer);

            //CRLFで分割
            var list = new List<string>(($"{receiveBuffer}{value}").Split("\r\n"));
            receiveBuffer = null;

            //最終行がCRLFで終わっていない場合
            if (list.Last() != string.Empty)
            {
                //最終行のみ次の受信イベントまでデータを引き継ぐ
                receiveBuffer = list.Last();
            }
            list.RemoveAt(list.Count - 1);
            foreach (var data in list)
            {
                _logger.LogTrace("<<{data}", data);
                //1行をイベント処理へ
                OnSerialReceived?.Invoke(this, data);
                if (data.StartsWith("EVENT"))
                {
                    OnEVENTReceived?.Invoke(this, new EVENT(data));
                }
                else if (data.StartsWith("ERXTCP"))
                {
                    OnERXTCPReceived?.Invoke(this, new ERXTCP(data));
                }
                else if (data.StartsWith("ERXUDP"))
                {
                    OnERXUDPReceived?.Invoke(this, new ERXUDP(data));
                }
                else if (data.StartsWith("ETCP"))
                {
                    OnETCPReceived?.Invoke(this, new ETCP(data));
                }
            }
        }

        private static readonly SemaphoreSlim CommandSemaphore = new(1, 1);

        private event EventHandler<string> OnSerialReceived;
        /// <summary>
        /// EVENT
        /// </summary>
        public event EventHandler<EVENT> OnEVENTReceived;
        /// <summary>
        /// TCP でデータを受信すると通知されます。
        /// </summary>
        public event EventHandler<ERXTCP> OnERXTCPReceived;
        /// <summary>
        /// 自端末宛ての UDP（マルチキャスト含む）を受信すると通知されます。
        /// </summary>
        public event EventHandler<ERXUDP> OnERXUDPReceived;
        /// <summary>
        /// TCP の接続、切断処理が発生すると通知されます。
        /// </summary>
        public event EventHandler<ETCP> OnETCPReceived;

        private async Task<TResponse> ExecuteSKCommandAsync<TResponse>(AbstractSKCommand<TResponse> command) where TResponse : class
        {
            //ほかのコマンドとの排他制御
            await CommandSemaphore.WaitAsync();

            var taskCompletionSource = new TaskCompletionSource<TResponse>();
            command.TaskCompletionSource = taskCompletionSource;
            OnSerialReceived += command.ReceiveHandler;
            try
            {
                //コマンド書き込み
                var commandBytes = command.GetCommandWithArgument();
                _logger.LogTrace(">>{Command}", command.GetCommandLogString());
                await serialPort.BaseStream.WriteAsync(commandBytes);

                //タイムアウト or コンプリート
                if (await Task.WhenAny(taskCompletionSource.Task, Task.Delay(command.Timeout)) == taskCompletionSource.Task)
                {
                    if (command.HasEchoback)
                    {
                        _logger.LogTrace("<<ECHO:{Command}", command.EchobackCommand);
                    }
                    return taskCompletionSource.Task.Result;
                }
                else
                {
                    if (command.HasEchoback)
                    {
                        _logger.LogTrace("<<ECHO:{Command}", command.EchobackCommand);
                    }
                    throw new TimeoutException("Timeout has expired");
                }
            }
            finally
            {
                OnSerialReceived -= command.ReceiveHandler;
                CommandSemaphore.Release();
            }
        }


        /// <summary>
        /// 現在の主要な通信設定値を表示します。
        /// </summary>
        /// <returns>EINFO</returns>
        public async Task<EINFO> SKInfoAsync()
        {
            return await ExecuteSKCommandAsync(new SKInfoCommand());
        }

        /// <summary>
        /// 指定したIPADDRに対して PaC（PANA 認証クライアント）として PANA 接続シーケンスを開始します。
        /// SKJOIN 発行前に PSK, PWD, Route-B ID 等のセキュリティ設定を施しておく必要があります。
        /// 接続先は SKSTART コマンドで PAA として動作開始している必要があります。
        /// 接続の結果はイベントで通知されます。
        /// PANA 接続シーケンスは PaC が PAA に対してのみ開始できます。
        /// 接続元（PaC）：
        ///  接続が完了すると、指定したIPADDRに対するセキュリティ設定が有効になり、以後の通信でデータが暗号化されます。
        /// 接続先（PAA）：
        ///  接続先はコーディネータとして動作開始している必要があります。
        ///  PSK から生成した暗号キーを自動的に配布します。
        ///  相手からの接続が完了すると接続元に対するセキュリティ設定が有効になり、以後の通信でデータが暗号化されます。
        ///  １つのデバイスとの接続が成立すると、他デバイスからの新規の接続を受け付けなくなります。
        /// </summary>
        /// <param name="ipaddr">
        /// 接続先 IP アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKJoinAsync(string ipaddr)
        {
            return await ExecuteSKCommandAsync(new SKJoinCommand(new SKJoinCommand.Input()
            {
                Ipaddr = ipaddr,
            }));
        }
        /// <summary>
        /// 64 ビット MAC アドレスを IPv6 リンクローカルアドレスに変換した結果を表示します。
        /// </summary>
        /// <param name="addr64">
        /// 64 ビット MAC アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<SKLL64> SKLl64Async(string addr64)
        {
            return await ExecuteSKCommandAsync(new SKLl64Command(new SKLl64Command.Input()
            {
                Addr64 = addr64,
            }));
        }

        /// <summary>
        /// 
        /// 指定したチャンネルに対してアクティブスキャン（IE あり）を実行します。
        /// アクティブスキャンは、PAN を発見する度に EPANDESC イベントが発生して内容が通知されます。その後、指定したすべてのチャンネルのスキャンが完了すると EVENT イベントが 0x1Eコードで発生して終了を通知します。
        /// Pairing 値(8 バイト)は S0A で設定します。
        /// Pairing ID が付与された拡張ビーコン要求を受信したコーディネータは、同じ Pairing 値が設定されている場合に、拡張ビーコンを応答します。
        /// MODE に 3 を指定すると、拡張ビーコン要求に Information Element を含めません。コーディネータは拡張ビーコンを応答します
        /// </summary>
        /// <param name="channelMask">
        /// スキャンするチャンネルをビットマップフラグで指定します。
        /// 最下位ビットがチャンネル 33 に対応します。
        /// </param>
        /// <param name="duration">
        /// 各チャンネルのスキャン時間を指定します。
        /// スキャン時間は以下の式で計算されます。
        /// 0.01 sec * (2^DURATION + 1)
        /// 値域：0-14
        /// </param>
        /// <returns>IEnumerable EPANDESC</returns>
        public async Task<IEnumerable<EPANDESC>> SKScanActiveExAsync(uint channelMask, byte duration)
        {
            var resp = await ExecuteSKCommandAsync(new SKScanCommand(new SKScanCommand.Input()
            {
                ScanMode = ScanMode.ActiveScanWithIE,
                ChannelMask = channelMask,
                Duration = duration,
            }));
            return resp.epandescs;
        }

        /// <summary>
        /// 指定した宛先に UDP でデータを送信します。
        /// SKSENDTO コマンドは以下の形式で正確に指定する必要があります。
        /// 1) アドレスは必ずコロン表記で指定してください。
        /// 2) ポート番号は必ず４文字指定してください。
        /// 3) データ長は必ず４文字指定してください。
        /// 4) セキュリティフラグは１文字で指定してください。
        /// 5) データは入力した内容がそのまま忠実にバイトデータとして扱われます。スペース、改行もそのままデータとして扱われます。
        /// 6) データは、データ長で指定したバイト数、必ず入力してください。サイズが足りないと、指定したバイト数揃うまでコマンド受け付け状態から抜けません。
        /// 7) データ部の入力はエコーバックされません。
        /// </summary>
        /// <param name="handle">
        /// 送信元 UDP ハンドル
        /// </param>
        /// <param name="ipaddr">
        /// 宛先 IPv6 アドレス
        /// </param>
        /// <param name="port">
        /// 宛先ポート番号
        /// </param>
        /// <param name="sec">
        /// 暗号化オプション
        /// </param>
        /// <param name="data">
        /// 送信データ
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSendToAsync(string handle, string ipaddr, string port, SKSendToSec sec, byte[] data)
        {
            return await ExecuteSKCommandAsync(new SKSendToCommand(new SKSendToCommand.Input()
            {
                Handle = handle,
                Ipaddr = ipaddr,
                Port = port,
                Sec = sec,
                Datalen = data.Length.ToString("X4"),
                Data = data,
            }));
        }

        /// <summary>
        /// PWD で指定したパスワードから PSK を生成して登録します。
        /// SKSETPSK による設定よりも本コマンドが優先され、PSK は本コマンドの内容で上書きされます。
        /// ＊）PWDの文字数が指定したLENに足りない場合、不足分は不定値になります。
        /// </summary>
        /// <param name="len">
        /// 1-32
        /// </param>
        /// <param name="pwd">
        /// ASCII 文字
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSetPwdAsync(string len, string pwd)
        {
            return await ExecuteSKCommandAsync(new SKSetPwdCommand(new SKSetPwdCommand.Input()
            {
                Len = len,
                Pwd = pwd,
            }));
        }

        /// <summary>
        /// 指定されたIDから各 Route-B ID を生成して設定します。
        /// Pairing ID (SA レジスタ)としてIDの下位 8 文字が設定されます。
        /// ＊）IDは ASCII 32 文字必要で、足りない場合、不足分が不定値になります。
        /// </summary>
        /// <param name="id">
        /// 32 桁の ASCII 文字
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSetRBIDAsync(string id)
        {
            return await ExecuteSKCommandAsync(new SKSetRBIDCommand(new SKSetRBIDCommand.Input()
            {
                Id = id,
            }));
        }

        /// <summary>
        /// 仮想レジスタの内容を表示・設定します。
        /// SREGに続けてVAL を指定すると値の設定、
        /// VALを指定しないとそのレジスタの現在値を表示します。
        /// 値の場合は ESREG イベントで通知されます。
        /// </summary>
        /// <param name="sreg">
        /// アルファベット‘S’で始まるレジスタ番号を１６進数で指定されます。
        /// </param>
        /// <param name="val">
        /// レジスタに設定する値
        /// 設定値域はレジスタ番号に依存します。
        /// </param>
        /// <returns>ESREG</returns>
        public async Task<ESREG> SKSRegAsync(string sreg, string val)
        {
            return await ExecuteSKCommandAsync(new SKSregCommand(new SKSregCommand.Input()
            {
                SReg = sreg,
                Val = val,
            }));
        }
    }
}
