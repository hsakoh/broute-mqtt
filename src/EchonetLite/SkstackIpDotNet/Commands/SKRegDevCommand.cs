using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// セキュリティを適用するため、指定した IP アドレスを端末に登録します。
    /// 登録数が上限の場合、FAIL ER10 が戻ります。
    /// </summary>
    internal class SKRegDevCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKRegDevCommand(Input input) : base("SKREGDEV")
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
            /// 登録対象となる IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
        }
    }
}
