using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// UDP の待ち受けポートを指定します。
    /// 設定したポートは、SKSAVE コマンドで保存した後、電源再投入時にオートロード機能でロードした場合に有効になります。
    /// </summary>
    internal class SKUdpPortCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKUdpPortCommand(Input input) : base("SKUDPPORT")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Handle} {Arg.Port}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 対応する UDP ハンドル番号（１－６）
            /// </summary>
            public string Handle { get; set; }
            /// <summary>
            /// ハンドル番号に割り当てられる待ち受けポート番号(0-0xFFFF)
            /// 0 を指定した場合は、そのハンドル番号のポートは着信しません。
            /// </summary>
            public string Port { get; set; }
        }
    }
}
