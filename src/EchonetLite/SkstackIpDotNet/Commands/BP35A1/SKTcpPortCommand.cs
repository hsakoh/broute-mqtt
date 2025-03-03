using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// TCP の待ち受けポートを指定します。
    /// 設定したポートは、SKSAVE コマンドで保存した後、電源再投入時にオートロード機能でロードした場合に有効になります。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKTcpPortCommand(SKTcpPortCommand.Input input) : SimpleCommand("SKTCPPORT")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Index} {Arg.Port}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 設定可能な４つのポートのどれを指定するかのインデックス（１－４）
            /// </summary>
            public string Index { get; set; }
            /// <summary>
            /// 設定する待ち受けポート番号(0-0xFFFF)
            /// 0 を指定した場合、そのハンドルは未使用となりポートは着信しません。また 0xFFFF は予約番号で着信しません。
            /// </summary>
            public string Port { get; set; }
        }
    }
}
