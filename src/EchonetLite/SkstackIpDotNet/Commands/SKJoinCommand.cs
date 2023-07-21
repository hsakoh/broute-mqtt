using System.Text;

namespace SkstackIpDotNet.Commands
{
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
    internal class SKJoinCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKJoinCommand(Input input) : base("SKJOIN", 15000)
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Ipaddr}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 接続先 IP アドレス
            /// </summary>
            public string Ipaddr { get; set; }
        }
    }
}
