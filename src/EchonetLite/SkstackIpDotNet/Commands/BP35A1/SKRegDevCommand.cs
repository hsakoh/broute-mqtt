using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// セキュリティを適用するため、指定した IP アドレスを端末に登録します。
    /// 登録数が上限の場合、FAIL ER10 が戻ります。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKRegDevCommand(SKRegDevCommand.Input input) : SimpleCommand("SKREGDEV")
    {
        private Input Arg { get; set; } = input;


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
