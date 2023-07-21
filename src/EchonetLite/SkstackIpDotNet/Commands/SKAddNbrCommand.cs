using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 指定した IP アドレスと 64bit アドレス情報を、IP 層のネイバーキャッシュに Reachable 状態で登録します。
    /// これによってアドレス要請を省略して直接 IP パケットを出力することができます。
    /// 同じ IP アドレスがエントリーされている場合は設定が上書きされます。
    /// ネイバーキャッシュがすでに一杯の場合は最も古いエントリーが削除され、ここで指定した IPアドレスが登録されます。
    /// </summary>
    internal class SKAddNbrCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKAddNbrCommand(Input input) : base("SKADDNBR")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Ipaddr} {Arg.Macaddr}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 登録する IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
            /// <summary>
            /// 登録 IPv6 アドレスに対応する 64bit アドレス
            /// </summary>
            public string Macaddr { get; set; }
        }
    }
}
