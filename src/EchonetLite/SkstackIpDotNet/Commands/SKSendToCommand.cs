using System.IO;
using System.Text;

namespace SkstackIpDotNet.Commands
{
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
    internal class SKSendToCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKSendToCommand(Input input) : base("SKSENDTO", 20000)
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                using (var ms = new MemoryStream())
                using(var bw = new BinaryWriter(ms))
                {

                    bw.Write(Encoding.ASCII.GetBytes($"{Arg.Handle} {Arg.Ipaddr} {Arg.Port} {(char)Arg.Sec} {Arg.Datalen} "));
                    bw.Write(Arg.Data);
                    return ms.ToArray();
                }
            }
        }

        internal override string GetCommandLogString()
        {
            return $"{Arg.Handle} {Arg.Ipaddr} {Arg.Port} {(char)Arg.Sec} {Arg.Datalen} ({BytesConvert.ToHexString(Arg.Data)})";
        }

        public class Input
        {
            /// <summary>
            /// 送信元 UDP ハンドル
            /// </summary>
            public string Handle { get; set; }
            /// <summary>
            /// 宛先 IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
            /// <summary>
            /// 宛先ポート番号
            /// </summary>
            public string Port { get; set; }
            /// <summary>
            /// 暗号化オプション
            /// 0: 必ず平文で送信
            /// 1: SKSECENABLE コマンドで送信先がセキュリティ有効で登録されている場合、暗号化して送ります。登録されてない場合、または、暗号化無しで登録されている場合、データは送信されません。
            /// 2: SKSECENABLE コマンドで送信先がセキュリティ有効で登録されている場合、暗号化して送ります。登録されてない場合、または、暗号化無しで登録されている場合、データは平文で送信されます。
            /// </summary>
            public SKSendToSec Sec { get; set; }
            /// <summary>
            /// 送信データ長
            /// </summary>
            public string Datalen { get; set; }
            /// <summary>
            /// 送信データ
            /// </summary>
            public byte[] Data { get; set; }
        }
    }
}
