using SkstackIpDotNet.Events;
using SkstackIpDotNet.Responses;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 指定したチャンネルに対してアクティブスキャンまたは ED スキャンを実行します。
    /// アクティブスキャンは、PAN を発見する度に EPANDESC イベントが発生して内容が通知されます。その後、指定したすべてのチャンネルのスキャンが完了すると EVENT イベントが 0x1Eコードで発生して終了を通知します。
    /// ED スキャンは、スキャンが完了した時点で EEDSCAN イベントが発生します。
    /// Pairing 値(8 バイト)は S0A で設定します。
    /// Pairing ID が付与された拡張ビーコン要求を受信したコーディネータは、同じ Pairing 値が設定されている場合に、拡張ビーコンを応答します。
    /// MODE に 3 を指定すると、拡張ビーコン要求に Information Element を含めません。コーディネータは拡張ビーコンを応答します
    /// </summary>
    internal class SKScanCommand : AbstractSKCommand<SKScanCommand.SKScanResponse>
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input"></param>
        public SKScanCommand(Input input) : base("SKSCAN")
        {
            Arg = input;
        }
        bool isResponseCommandEndReceived = false;
        bool isEEDSCANReceiveStart = false;
        List<string> eventBufferEPANDESC = null;
        bool isEPANDESCReceiveStart = false;
        readonly SKScanResponse response = new();
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("OK"))
            {
                isResponseCommandEndReceived = true;
            }
            else if (eventRow.StartsWith("FAIL"))
            {
                throw new Exception($"Command Fail {eventRow}");
            }
            else if (eventRow.StartsWith("EVENT"))//0x1F or 0x22
            {
                response.@event = new EVENT(eventRow);
                //EVENTより後にEPANDESCが来ることがあるので、1秒後に終わらせる
                Task.Delay(500).ContinueWith((t) =>
                {
                    if (isResponseCommandEndReceived)
                    {
                        TaskCompletionSource.TrySetResult(response);
                    }
                });

            }
            else if (eventRow.StartsWith("EEDSCAN"))
            {
                isEEDSCANReceiveStart = true;
            }
            else if (isEEDSCANReceiveStart)
            {
                response.eedscan = new EEDSCAN(eventRow);
                isEEDSCANReceiveStart = false;
            }
            else if (eventRow.StartsWith("EPANDESC"))
            {
                isEPANDESCReceiveStart = true;
                eventBufferEPANDESC = new List<string>
                {
                    eventRow
                };
            }
            else if (isEPANDESCReceiveStart)
            {
                eventBufferEPANDESC.Add(eventRow);
                if (eventBufferEPANDESC.Count == 7 && Arg.ScanMode == ScanMode.ActiveScanWithIE)
                {
                    response.epandescs.Add(new EPANDESC(string.Join("\r\n", eventBufferEPANDESC)));
                    eventBufferEPANDESC.Clear();
                    isEPANDESCReceiveStart = false;
                }
                if (eventBufferEPANDESC.Count == 6 && Arg.ScanMode == ScanMode.ActiveScanWithoutIE)
                {
                    response.epandescs.Add(new EPANDESC(string.Join("\r\n", eventBufferEPANDESC)));
                    eventBufferEPANDESC.Clear();
                    isEPANDESCReceiveStart = false;
                }
            }
        }
        public class Input
        {

            /// <summary>
            /// 0:ED スキャン
            /// 2:アクティブスキャン（IE あり）
            /// 3:アクティブスキャン（IE なし）
            /// </summary>
            public ScanMode ScanMode { get; set; }
            /// <summary>
            /// スキャンするチャンネルをビットマップフラグで指定します。
            /// 最下位ビットがチャンネル 33 に対応します。
            /// </summary>
            public uint ChannelMask { get; set; }
            /// <summary>
            /// 各チャンネルのスキャン時間を指定します。
            /// スキャン時間は以下の式で計算されます。
            /// 0.01 sec * (2^DURATION + 1)
            /// 値域：0-14
            /// </summary>
            public byte Duration { get; set; }
        }
        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get { return Encoding.ASCII.GetBytes($"{(Char)Arg.ScanMode} {BytesConvert.ToHexString(BitConverter.GetBytes(Arg.ChannelMask))} {Arg.Duration}"); }
        }
        internal override int Timeout => (int)(Math.Pow(2, Arg.Duration) + 1) * 10 * (60 - 33) + 3000;

        public class SKScanResponse
        {
            public List<EPANDESC> epandescs = new();
            public EEDSCAN eedscan = null;
            public EVENT @event = null;
        }
    }
}
