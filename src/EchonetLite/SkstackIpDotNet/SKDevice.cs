using Microsoft.Extensions.Logging;
using SkstackIpDotNet.Commands;
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
    public class SKDevice : IDisposable
    {
        private readonly ILogger _logger;
        private SerialPort serialPort;
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="logger">logger</param>
        public SKDevice(ILogger<SKDevice> logger)
        {
            _logger = logger;
        }
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
        /// 指定した IP アドレスと 64bit アドレス情報を、IP 層のネイバーキャッシュに Reachable 状態で登録します。
        /// これによってアドレス要請を省略して直接 IP パケットを出力することができます。
        /// 同じ IP アドレスがエントリーされている場合は設定が上書きされます。
        /// ネイバーキャッシュがすでに一杯の場合は最も古いエントリーが削除され、ここで指定した IPアドレスが登録されます。
        /// </summary>
        /// <param name="ipaddr">
        /// 登録する IPv6 アドレス
        /// </param>
        /// <param name="macaddr">
        /// 登録 IPv6 アドレスに対応する 64bit アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKAddNbrAsync(string ipaddr, string macaddr)
        {
            return await ExecuteSKCommandAsync(new SKAddNbrCommand(new SKAddNbrCommand.Input()
            {
                Ipaddr = ipaddr,
                Macaddr = macaddr,
            }));
        }

        /// <summary>
        /// 指定したハンドルに対応する TCP コネクションの切断要求を発行します。
        /// 切断処理の結果は ETCP イベントで通知されます。
        /// すでに切断初折が実行中の場合、本コマンドを発行すると FAIL ER10 になります。
        /// </summary>
        /// <param name="handle">
        /// ハンドル番号
        /// SKCONNECT で接続を確立した際に受け取ったハンドル番号を指定します。
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKCloseAsync(string handle)
        {
            return await ExecuteSKCommandAsync(new SKCloseCommand(new SKCloseCommand.Input()
            {
                Handle = handle
            }));
        }

        /// <summary>
        ///指定した宛先に TCP の接続要求を発行します。
        ///相手側は指定したポートで TCP の接続待ち受けを開始している必要があります。
        ///接続処理の結果は ETCP イベントで通知されます。接続に成功した場合、ETCP イベントでコネクションに対応するハンドル番号が通知されます。
        ///以後、データ送信や切断処理はこのハンドル番号を指定します。
        ///同じLPORTとRPORTの組み合わせで E すでに何らかの宛先と接続が確立している場合、ER10 エラーとなります。
        ///このためLPORTは 0 以外のランダムな数値を指定することを推奨します。
        ///( LPORTは自端末の待受ポート番号である必要はありません)
        ///接続処理の実行途中に本コマンドが発行されると ER10 エラーとなります。
        ///指定可能な待ち受け数とポート番号の初期値はリリースノートをご参照ください。
        /// </summary>
        /// <param name="ipAddr">接続先 IPv6 アドレス</param>
        /// <param name="rPort">接続先ポート番号 値域：1-65535</param>
        /// <param name="lPort">接続元ポート番号 値域：1-65535</param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKConnect(string ipAddr, string rPort, string lPort)
        {
            return await ExecuteSKCommandAsync(new SKConnectCommand(new SKConnectCommand.Input()
            {
                Ipaddr = ipAddr,
                RPort = rPort,
                LPort = lPort,
            }));
        }

        /// <summary>
        /// レジスタ保存用の不揮発性メモリエリアを初期化して、未保存状態に戻します。
        /// 本コマンドを実行後、SKLOAD コマンドを発行すると SKLOAD コマンドは ER10 エラーを返すようになります。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKErase()
        {
            return await ExecuteSKCommandAsync(new SKEraseCommand());
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
        /// 不揮発性メモリに保存されている仮想レジスタの内容をロードします。
        /// 何らかの要因でロードに失敗した場合、FAIL ER10 エラーになります。
        /// 内容が保存されていない状態でロードを実行すると L ER10 になります。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKLoadAsync()
        {
            return await ExecuteSKCommandAsync(new SKLoadCommand());
        }
        /// <summary>
        /// 指定した IPv6 宛てに ICMP Echo request を送信します。
        /// Echo reply を受信すると EPONG イベントで通知されます。
        /// </summary>
        /// <param name="ipaddr">
        /// Ping 送信先の IPv6 アドレス
        /// </param>
        /// <returns>EPONG</returns>
        public async Task<EPONG> SKPingAsync(string ipaddr)
        {
            return await ExecuteSKCommandAsync(new SKPingCommand(new SKPingCommand.Input()
            {
                Ipaddr = ipaddr,
            }));
        }
        /// <summary>
        /// セキュリティを適用するため、指定した IP アドレスを端末に登録します。
        /// 登録数が上限の場合、FAIL ER10 が戻ります。
        /// </summary>
        /// <param name="ipaddr">
        /// 登録対象となる IPv6 アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKRegDevAsync(string ipaddr)
        {
            return await ExecuteSKCommandAsync(new SKRegDevCommand(new SKRegDevCommand.Input()
            {
                Ipaddr = ipaddr,
            }));
        }
        /// <summary>
        /// 現在接続中の相手に対して再認証シーケンスを開始します。
        /// 再認証シーケンスの前に SKJOIN による接続が成功している必要があり、接続していないとER10 になります。
        /// 再認証に成功すると、暗号キーと PANA セッション期限が更新されます。
        /// PaC は、PAA が指定したセッションのライフタイムの 80%が経過した時点で、自動的に再認証シーケンスを実行します。
        /// このため SKREJOIN コマンドは基本的に発行する必要がありませんが、任意のタイミングで再認証したい場合には本コマンドを使います。
        /// PAA は、セッションが更新されずにライフタイムが過ぎるとセッション終了要請を自動的に発行します。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKRejoinAsync()
        {
            return await ExecuteSKCommandAsync(new SKRejoinCommand());
        }

        /// <summary>
        /// プロトコル・スタックの内部状態を初期化します。
        /// すべての内部変数が初期値に戻ります。
        /// ただし 64bit アドレスだけは、S01 レジスタで設定した直近の値がそのまま再利用されます。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKResetAsync()
        {
            return await ExecuteSKCommandAsync(new SKResetCommand());
        }

        /// <summary>
        /// プロトコル・スタックの内部状態を初期化します。
        /// すべての内部変数が初期値に戻ります。
        /// ただし 64bit アドレスだけは、S01 レジスタで設定した直近の値がそのまま再利用されます。
        /// </summary>
        /// <param name="target">
        /// 削除したいエントリーの IPv6 アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKRmDevAsync(string target)
        {
            return await ExecuteSKCommandAsync(new SKRmDevCommand(new SKRmDevCommand.Input()
            {
                Target = target
            }));
        }
        /// <summary>
        /// 現在の仮想レジスタの内容を不揮発性メモリに保存します。
        /// 何らかの要因で保存に失敗した場合、FAIL ER10 エラーになります。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSaveAsync()
        {
            return await ExecuteSKCommandAsync(new SKSaveCommand());
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
        /// 指定したチャンネルに対してアクティブスキャン（IE なし）を実行します。
        /// アクティブスキャンは、PAN を発見する度に EPANDESC イベントが発生して内容が通知されます。その後、指定したすべてのチャンネルのスキャンが完了すると EVENT イベントが 0x1Eコードで発生して終了を通知します。
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
        public async Task<IEnumerable<EPANDESC>> SKScanActiveAsync(uint channelMask, byte duration)
        {
            var resp = await ExecuteSKCommandAsync(new SKScanCommand(new SKScanCommand.Input()
            {
                ScanMode = ScanMode.ActiveScanWithoutIE,
                ChannelMask = channelMask,
                Duration = duration,
            }));
            return resp.epandescs;
        }
        /// <summary>
        /// 指定したチャンネルに対してED スキャンを実行します。
        /// ED スキャンは、スキャンが完了した時点で EEDSCAN イベントが発生します。
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
        /// <returns>EEDSCAN</returns>
        public async Task<EEDSCAN> SKScanEdAsync(uint channelMask, byte duration)
        {
            var resp = await ExecuteSKCommandAsync(new SKScanCommand(new SKScanCommand.Input()
            {
                ScanMode = ScanMode.EdScan,
                ChannelMask = channelMask,
                Duration = duration,
            }));
            return resp.eedscan;
        }

        /// <summary>
        /// 指定した IP アドレスに対する MAC 層セキュリティの有効・無効を指定します。
        /// 指定する IPADDR は、事前に SKREGDEV コマンドで登録されている必要があります。登録されていない IP アドレスを指定すると FAIL ER10 になります。
        /// MODEが 1 の場合、指定したIPADDRに対するMACADDR情報が更新されます。
        /// MODE=1 で登録に成功した場合、このMACADDRに対応する MAC 層フレームカウンタが0 で初期化されます。
        /// MODEが 0 の場合、セキュリティの適用が無効になるだけで、MACADDR情報は更新されません（値は無視されます）。
        /// 本コマンドによるセキュリティ設定は送信時に適用されるものです。受信時は、受信した MACフレームの内容により必要な復号化が行われます。
        /// </summary>
        /// <param name="mode">
        /// 0:セキュリティ無効
        /// 1:セキュリティ適用
        /// </param>
        /// <param name="ipaddr">
        /// セキィリティを適用する対象の IPv6 アドレス
        /// </param>
        /// <param name="macaddr">
        /// 対象 IPv6 アドレスに対応する 64bit アドレス
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSecAsync(string mode, string ipaddr, string macaddr)
        {
            return await ExecuteSKCommandAsync(new SKSecEnableCommand(new SKSecEnableCommand.Input()
            {
                Mode = mode,
                Ipaddr = ipaddr,
                Macaddr = macaddr,
            }));
        }

        /// <summary>
        /// 指定したハンドル番号に対応する TCP コネクションを介して接続相手にデータを送信します。
        /// 送信処理の結果は ETCP イベントで通知されます。
        /// すでにデータが送信中の場合、本コマンドを発行すると FAIL ER10 になります。
        /// SKSEND は以下の形式で正確に指定する必要があります。
        /// 1) アドレスは必ずコロン表記で指定してください。
        /// 2) ポート番号は必ず４文字指定してください。
        /// 3) データ長は必ず４文字指定してください。
        /// 4) データは入力した内容がそのまま忠実にバイトデータとして扱われます。
        /// スペース、改行もそのままデータとして扱われます。
        /// 5) データは、データ長で指定したバイト数、必ず入力してください。サイズが足りないと、指定したバイト数揃うまでコマンド受け付け状態から抜けません。
        /// 6) データ部の入力はエコーバックされません。
        /// 正しい例：
        /// SKSEND 1 0005 01234
        /// (“01234”は画面にエコーバックされません)
        /// ターミナルソフトで入力した場合、5 バイトで 0x30, 0x31. 0x32, 0x33, 0x34 が送信されます。
        /// 受信側では、受信データの 16 進数 ASCII 表現で表示されます。
        /// </summary>
        /// <param name="handle">
        /// ハンドル番号
        /// SKCONNECT で接続を確立した際に受け取ったハンドル番号を指定します。
        /// </param>
        /// <param name="data">
        /// 送信データ
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSendAsync(string handle, byte[] data)
        {
            return await ExecuteSKCommandAsync(new SKSendCommand(new SKSendCommand.Input()
            {
                Handle = handle,
                Datalen = data.Length.ToString("X4"),
                Data = data,
            }));
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
        /// 指定されたキーインデックスに対する暗号キー(128bit)を、MAC 層セキュリティコンポーネントに登録します。
        /// 本コマンドでキーを設定後、SKSECENABLE コマンドで対象デバイスのセキュリティ設定を有効にすることで、以後、そのデバイスに対するユニキャストが MAC 層で暗号化されます。
        /// 指定したキーの桁が 16 バイト（ASCII 32 文字）より多い場合、ER06 になります。
        /// 桁が 16 バイトより少ない場合、キーの内容が不定になり、OK または FAIL どちらになるか不定です。必ず 32 文字を指定してください。
        /// 暗号キーの登録容量を超えている場合、FAIL ER10 になります。指定したキーインデックスに既存の設定がある場合、新しい設定で上書き登録されます。
        /// 登録に成功するとカレントキーインデックスが指定したINDEXに切り替わります。
        /// </summary>
        /// <param name="index">
        /// キーインデックス
        /// </param>
        /// <param name="key">
        /// 128bit NWK 暗号キー
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSetKey(string index, string key)
        {
            return await ExecuteSKCommandAsync(new SKSetKeyCommand(new SKSetKeyCommand.Input()
            {
                Index = index,
                Key = key,
            }));
        }

        /// <summary>
        /// PANA 認証に用いる PSK を登録します。
        /// すでに PSK が登録されている場合は新しい値で上書きされます。
        /// KEYのバイト列は ASCII２文字で１バイトの HEX 表現で指定します。そのためLENで指定する PSK 長の２倍の文字入力が必要です。
        /// PSK を変更するには、SKRESET でリセットした後、再度、SKSETPSK コマンドを発行する必要があります。
        /// ＊）PSK は 16 バイトの必要があります。そのため LEN は 0x10 以外で FAIL ER06 になります。
        /// またKEYが 32 文字（１６バイト）に足りない場合は、不足分が不定値になります。
        /// </summary>
        /// <param name="len">
        /// PSK のバイト長
        /// </param>
        /// <param name="key">
        /// PSK バイト列
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKSetPskAsync(string len, string key)
        {
            return await ExecuteSKCommandAsync(new SKSetPskCommand(new SKSetPskCommand.Input()
            {
                Len = len,
                Key = key,
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

        /// <summary>
        /// 端末を PAA (PANA 認証サーバ)として動作開始します。
        /// 動作開始に先立って PSK, PWD, Route-B ID 等のセキュリティ設定を済ませておく必要があります。
        /// 本コマンドを発行すると自動的にコーディネータとして動作開始し S15 レジスタ値は１になります。
        /// またアクティブスキャンに対して自動的に応答するようになります。
        /// 本コマンド発行前に確立していた PANA セッションはクリアされます。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKStartAsync()
        {
            return await ExecuteSKCommandAsync(new SKStartCommand());
        }

        /// <summary>
        /// SKSTACK IP 内の各種テーブル内容を画面表示します。
        /// 自端末で利用可能な IP アドレス一覧
        /// </summary>
        /// <returns>EADDR</returns>
        public async Task<EADDR> SKTableEAddrAsync()
        {
            return (EADDR)await ExecuteSKCommandAsync(new SKTableCommand(SKTableMode.EAddr));
        }
        /// <summary>
        /// SKSTACK IP 内の各種テーブル内容を画面表示します。
        /// 自端末で利用可能な IP アドレス一覧
        /// </summary>
        /// <returns>ENEIGHBOR</returns>
        public async Task<ENEIGHBOR> SKTableENeighborAsync()
        {
            return (ENEIGHBOR)await ExecuteSKCommandAsync(new SKTableCommand(SKTableMode.ENeighbor));
        }
        /// <summary>
        /// SKSTACK IP 内の各種テーブル内容を画面表示します。
        /// ネイバーキャッシュ
        /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
        /// </summary>
        /// <returns>ENBR</returns>
        public async Task<ENBR> SKTableENbrAsync()
        {
            return (ENBR)await ExecuteSKCommandAsync(new SKTableCommand(SKTableMode.ENbr));
        }
        /// <summary>
        /// SKSTACK IP 内の各種テーブル内容を画面表示します。
        /// ネイバーテーブル一覧
        /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
        /// </summary>
        /// <returns>ESEC</returns>
        public async Task<ESEC> SKTableESecAsync()
        {
            return (ESEC)await ExecuteSKCommandAsync(new SKTableCommand(SKTableMode.ESec));
        }
        /// <summary>
        /// SKSTACK IP 内の各種テーブル内容を画面表示します。
        /// TCP ハンドル状態一覧
        /// </summary>
        /// <returns>EHANDLE</returns>
        public async Task<EHANDLE> SKTableEHandleAsync()
        {
            return (EHANDLE)await ExecuteSKCommandAsync(new SKTableCommand(SKTableMode.EHandle));
        }

        /// <summary>
        /// TCP の待ち受けポートを指定します。
        /// 設定したポートは、SKSAVE コマンドで保存した後、電源再投入時にオートロード機能でロードした場合に有効になります。
        /// </summary>
        /// <param name="index">
        /// 設定可能な４つのポートのどれを指定するかのインデックス（１－４）
        /// </param>
        /// <param name="port">
        /// 設定する待ち受けポート番号(0-0xFFFF)
        /// 0 を指定した場合、そのハンドルは未使用となりポートは着信しません。また 0xFFFF は予約番号で着信しません。
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKTcpPortAsync(string index, string port)
        {
            return await ExecuteSKCommandAsync(new SKTcpPortCommand(new SKTcpPortCommand.Input()
            {
                Index = index,
                Port = port,
            }));
        }

        /// <summary>
        /// 現在確立している PANA セッションの終了を要請します。
        /// SKTERM は PAA、PaC どちらからでも実行できます。接続が確立していない状態でコマンドを発行すると ER10 になります。
        /// セッションの終了に成功すると暗号通信は解除されます。
        /// また PAA は他デバイスからの新しい接続を受け入れることができるようになります。
        /// セッションの終了要請に対して相手から応答がなく EVENT 28 が発生した場合、セッションは終了扱いとなります。
        /// </summary>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKTermAsync()
        {
            return await ExecuteSKCommandAsync(new SKTermCommand());
        }

        /// <summary>
        /// UDP の待ち受けポートを指定します。
        /// 設定したポートは、SKSAVE コマンドで保存した後、電源再投入時にオートロード機能でロードした場合に有効になります。
        /// </summary>
        /// <param name="handle">
        /// 対応する UDP ハンドル番号（１－６）
        /// </param>
        /// <param name="port">
        /// ハンドル番号に割り当てられる待ち受けポート番号(0-0xFFFF)
        /// 0 を指定した場合は、そのハンドル番号のポートは着信しません。
        /// </param>
        /// <returns>OKorFAIL</returns>
        public async Task<OKorFAIL> SKUdpPortAsync(string handle, string port)
        {
            return await ExecuteSKCommandAsync(new SKUdpPortCommand(new SKUdpPortCommand.Input()
            {
                Handle = handle,
                Port = port,
            }));
        }

        /// <summary>
        /// SKSTACK IP のファームウェアバージョンを表示します。
        /// EVER イベントが発生します。
        /// </summary>
        /// <returns>EVER</returns>
        public async Task<EVER> SKVerAsync()
        {
            return await ExecuteSKCommandAsync(new SKVerCommand());
        }
    }
}
