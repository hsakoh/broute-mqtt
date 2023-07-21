using SkstackIpDotNet.Responses;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SkstackIpDotNet.Commands
{

    internal abstract class AbstractSKCommand<TResponse> where TResponse : class
    {
        public AbstractSKCommand(string command, int timeout = 5000)
        {
            Command = command;
            Timeout = timeout;
        }
        internal TaskCompletionSource<TResponse> TaskCompletionSource { get; set; }
        public virtual void ReceiveHandler(object sendor, string eventRow)
        {
            if (eventRow.StartsWith(Command))
            {
                //エコーバック
                HasEchoback = true;
                EchobackCommand = eventRow;
            }
        }
        internal bool HasEchoback { get; set; } = false;
        internal string EchobackCommand { get; private set; } = null;
        internal virtual byte[] Arguments { get; private set; } = null;
        internal virtual int Timeout { get; private set; }
        internal string Command { get; private set; }

        internal virtual byte[] GetCommandWithArgument()
        {
            using (var ms = new MemoryStream())
            using (var bw = new BinaryWriter(ms))
            {
                bw.Write(Encoding.ASCII.GetBytes(Command));
                if (Arguments != null)
                {
                    bw.Write(Encoding.ASCII.GetBytes(" "));
                    bw.Write(Arguments);
                }
                bw.Write(Encoding.ASCII.GetBytes("\r\n"));
                return ms.ToArray();
            }
        }

        internal virtual string GetCommandLogString()
        {
            return Encoding.ASCII.GetString(GetCommandWithArgument());
        }
    }
    internal class SimpleCommand : AbstractSKCommand<OKorFAIL>
    {
        public SimpleCommand(string command, int timeout = 2000) : base(command, timeout)
        {

        }
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("OK"))
            {
                TaskCompletionSource.SetResult(new OK(eventRow));
            }
            else if (eventRow.StartsWith("FAIL"))
            {
                TaskCompletionSource.SetResult(new FAIL(eventRow));
            }
        }
    }
}
