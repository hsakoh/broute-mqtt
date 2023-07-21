using SkstackIpDotNet.Responses;

namespace SkstackIpDotNet.Commands
{

    /// <summary>
    /// 現在の主要な通信設定値を表示します。
    /// </summary>
    internal class SKInfoCommand : AbstractSKCommand<EINFO>
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SKInfoCommand() : base("SKINFO")
        {

        }

        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        EINFO response = null;
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("EINFO"))
            {
                isResponseBodyReceived = true;
                response = new EINFO(eventRow);
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
