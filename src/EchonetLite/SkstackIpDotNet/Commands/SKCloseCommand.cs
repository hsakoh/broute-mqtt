using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 指定したハンドルに対応する TCP コネクションの切断要求を発行します。
    /// 切断処理の結果は ETCP イベントで通知されます。
    /// すでに切断初折が実行中の場合、本コマンドを発行すると FAIL ER10 になります。
    /// </summary>
    internal class SKCloseCommand : SimpleCommand
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input">入力</param>
        public SKCloseCommand(Input input) : base("SKCLOSE")
        {
            Arg = input;
        }

        private Input Arg { get; set; }


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Handle}");
            }
        }

        public class Input
        {
            /// <summary>
            /// ハンドル番号
            /// SKCONNECT で接続を確立した際に受け取ったハンドル番号を指定します。
            /// </summary>
            public string Handle { get; set; }
        }
    }
}
