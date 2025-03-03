using SkstackIpDotNet.Responses;
using System.Net;
using System.Text;

namespace SkstackIpDotNet.Commands.BP35C0
{
    /// <summary>
    /// 64 ビット MAC アドレスを IPv6 リンクローカルアドレスに変換した結果を表示します。
    /// </summary>
    internal class SKLl64Command(SKLl64Command.Input input) : AbstractSKCommand<SKLL64>("SKLL64")
    {
        private Input Arg { get; set; } = input;


        internal override byte[] Arguments
        {
            get
            {
                return Encoding.ASCII.GetBytes($"{Arg.Addr64}");
            }
        }

        public class Input
        {
            /// <summary>
            /// 64 ビット MAC アドレス
            /// </summary>
            public string Addr64 { get; set; }
        }

        public override void ReceiveHandler(object sendor, string eventRow)
        {
            if (HasEchoback)
            {
                //エコーバックの次に、<IPADDR><CRLF>が来る
                TaskCompletionSource.SetResult(new SKLL64(eventRow));
            }
            else if (IPAddress.TryParse(eventRow, out _))
            {
                //エコーバックが無い場合で、<IPADDR><CRLF>として解釈できる行の場合は、それをもって結果を確定する
                TaskCompletionSource.SetResult(new SKLL64(eventRow));
            }
            base.ReceiveHandler(sendor, eventRow);
        }
    }
}
