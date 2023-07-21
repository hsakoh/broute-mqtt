using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 指定した IP アドレスのエントリーをネイバーテーブル、ネイバーキャッシュから強制的に削除します。
    /// </summary>
    internal class SKRmDevCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKRmDevCommand(Input input) : base("SKRMDEV")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


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
