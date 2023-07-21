using SkstackIpDotNet.Responses;

namespace SkstackIpDotNet.Commands
{
    /// <summary>
    /// SKSTACK IP のファームウェアバージョンを表示します。
    /// EVER イベントが発生します。
    /// </summary>
    internal class SKVerCommand : AbstractSKCommand<EVER>
    {
        public SKVerCommand() : base("SKVER")
        {

        }

        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        EVER response = null;
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("EVER"))
            {
                isResponseBodyReceived = true;
                response = new EVER(eventRow);
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
    }
}
