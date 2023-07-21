using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    ///指定した宛先に TCP の接続要求を発行します。
    ///相手側は指定したポートで TCP の接続待ち受けを開始している必要があります。
    ///接続処理の結果は ETCP イベントで通知されます。接続に成功した場合、ETCP イベントでコネクションに対応するハンドル番号が通知されます。
    ///以後、データ送信や切断処理はこのハンドル番号を指定します。
    ///同じLPORTとRPORTの組み合わせで E すでに何らかの宛先と接続が確立している場合、ER10 エラーとなります。
    ///このためLPORTは 0 以外のランダムな数値を指定することを推奨します。
    ///( LPORTは自端末の待受ポート番号である必要はありません)
    ///接続処理の実行途中に本コマンドが発行されると ER10 エラーとなります。
    ///指定可能な待ち受け数とポート番号の初期値はリリースノートをご参照ください
    /// </summary>
    internal class SKConnectCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKConnectCommand(Input input) : base("SKCONNECT")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Ipaddr} {Arg.RPort} {Arg.LPort}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 接続先 IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
            /// <summary>
            /// 接続先ポート番号 値域：1-65534
            /// </summary>
            public string RPort { get; set; }
            /// <summary>
            /// 接続元ポート番号 値域：1-65534
            /// </summary>
            public string LPort { get; set; }
        }
    }
}
