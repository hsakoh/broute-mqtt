using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// 指定した IP アドレスに対する MAC 層セキュリティの有効・無効を指定します。
    /// 指定する IPADDR は、事前に SKREGDEV コマンドで登録されている必要があります。登録されていない IP アドレスを指定すると FAIL ER10 になります。
    /// MODEが 1 の場合、指定したIPADDRに対するMACADDR情報が更新されます。
    /// MODE=1 で登録に成功した場合、このMACADDRに対応する MAC 層フレームカウンタが0 で初期化されます。
    /// MODEが 0 の場合、セキュリティの適用が無効になるだけで、MACADDR情報は更新されません（値は無視されます）。
    /// 本コマンドによるセキュリティ設定は送信時に適用されるものです。受信時は、受信した MACフレームの内容により必要な復号化が行われます。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKSecEnableCommand(SKSecEnableCommand.Input input) : SimpleCommand("SKSECENABLE")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Mode} {Arg.Ipaddr} {Arg.Macaddr}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 0:セキュリティ無効
            /// 1:セキュリティ適用
            /// </summary>
            public string Mode { get; set; }
            /// <summary>
            /// セキィリティを適用する対象の IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
            /// <summary>
            /// 対象 IPv6 アドレスに対応する 64bit アドレス
            /// </summary>
            public string Macaddr { get; set; }
        }
    }
}
