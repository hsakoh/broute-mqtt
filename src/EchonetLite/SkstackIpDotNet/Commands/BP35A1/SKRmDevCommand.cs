using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// 指定した IP アドレスのエントリーをネイバーテーブル、ネイバーキャッシュから強制的に削除します。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKRmDevCommand(SKRmDevCommand.Input input) : SimpleCommand("SKRMDEV")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Target}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 削除したいエントリーの IPv6 アドレス
            /// </summary>
            public string Target { get; set; }
        }
    }
}
