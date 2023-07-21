using SkstackIpDotNet.Responses;
using System.Text;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// 仮想レジスタの内容を表示・設定します。
    /// SREGに続けてVAL を指定すると値の設定、
    /// VALを指定しないとそのレジスタの現在値を表示します。
    /// 値の場合は ESREG イベントで通知されます。
    /// </summary>
    internal class SKSregCommand : AbstractSKCommand<ESREG>
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="input"></param>
        public SKSregCommand(Input input) : base("SKSREG")
        {
            Arg = input;
        }

        private Input Arg { get; set; }

        public class Input
        {
            /// <summary>
            /// アルファベット‘S’で始まるレジスタ番号を１６進数で指定されます。
            /// </summary>
            public string SReg { get; set; }
            /// <summary>
            /// レジスタに設定する値
            /// 設定値域はレジスタ番号に依存します。
            /// </summary>
            public string Val { get; set; }
        }

        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        ESREG response = null;
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("OK"))
            {
                isResponseCommandEndReceived = true;
            }
            else if (eventRow.StartsWith("ESREG"))
            {
                isResponseBodyReceived = true;
                response = new ESREG(eventRow);
            }

            //Valがある場合、OKでおわり
            if (isResponseCommandEndReceived
                && (isResponseBodyReceived || Arg.Val != null))
            {
                TaskCompletionSource.SetResult(response);
            }
        }

        internal override byte[] Arguments
        {
            get
            {
                if (Arg.Val == null)
                {
                    return Encoding.ASCII.GetBytes($"{Arg.SReg}");

                }
                else
                {
                    return Encoding.ASCII.GetBytes($"{Arg.SReg} {Arg.Val}");
                }
            }
        }
    }
}
