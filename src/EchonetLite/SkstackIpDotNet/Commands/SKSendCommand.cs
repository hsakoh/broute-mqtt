using System.IO;
using System.Text;

namespace SkstackIpDotNet.Commands
{
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
    internal class SKSendCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKSendCommand(Input input) : base("SKSEND")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                using (var ms = new MemoryStream())
                using (var bw = new BinaryWriter(ms))
                {

                    bw.Write(Encoding.ASCII.GetBytes($"{Arg.Handle} {Arg.Datalen} "));
                    bw.Write(Arg.Data);
                    return ms.ToArray();
                }
            }
        }

        internal override string GetCommandLogString()
        {
            return $"{Arg.Handle} {Arg.Datalen} ({BytesConvert.ToHexString(Arg.Data)})";
        }

        public class Input
        {
            /// <summary>
            /// ハンドル番号
            /// SKCONNECT で接続を確立した際に受け取ったハンドル番号を指定します。
            /// </summary>
            public string Handle { get; set; }
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
