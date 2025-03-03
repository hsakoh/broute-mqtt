using SkstackIpDotNet.Responses;
using System.Text;

namespace SkstackIpDotNet.Commands.BP35A1
{
    /// <summary>
    /// 指定した IPv6 宛てに ICMP Echo request を送信します。
    /// Echo reply を受信すると EPONG イベントで通知されます。
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="input">入力</param>
    internal class SKPingCommand(SKPingCommand.Input input) : AbstractSKCommand<EPONG>("SKPING")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Ipaddr}");
            }
        }

        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        EPONG response = null;
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("EPONG"))
            {
                isResponseBodyReceived = true;
                response = new EPONG(eventRow);
            }
            else if (eventRow.StartsWith("OK"))
            {
                isResponseCommandEndReceived = true;
            }
            if (isResponseBodyReceived && isResponseCommandEndReceived)
            {
                TaskCompletionSource.SetResult(response);
            }
        }

        public class Input
        {
            /// <summary>
            /// Ping 送信先の IPv6 アドレス
            /// </summary>
            public string Ipaddr { get; set; }
        }
    }
}
