﻿using SkstackIpDotNet.Responses.Table;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SkstackIpDotNet.Commands.BP35A1
{

    /// <summary>
    /// SKSTACK IP 内の各種テーブル内容を画面表示します。
    /// 表示するテーブルに対応したイベントが発生します。
    /// </summary>
    internal class SKTableCommand(SKTableMode mode) : AbstractSKCommand<BaseTableResponse>("SKTABLE")
    {
        private SKTableMode Mode { get; set; } = mode;
        bool isResponseBodyReceived = false;
        bool isResponseCommandEndReceived = false;
        bool isEventStart = false;
        List<string> eventBuffer = null;
        BaseTableResponse response = null;
        public override void ReceiveHandler(object sendor, string eventRow)
        {
            base.ReceiveHandler(sendor, eventRow);
            if (eventRow.StartsWith("OK"))
            {
                isResponseCommandEndReceived = true;
                if (isResponseBodyReceived && isResponseCommandEndReceived)
                {
                    TaskCompletionSource.TrySetResult(response);
                }
            }
            else if (eventRow.StartsWith("EADDR")
                || eventRow.StartsWith("ENEIGHBOR")
                || eventRow.StartsWith("ENBR")
                || eventRow.StartsWith("ESEC")
                || eventRow.StartsWith("EHANDLE"))
            {
                isEventStart = true;
                eventBuffer = [eventRow];
            }
            else if (isEventStart)
            {
                eventBuffer.Add(eventRow);
                if (Mode == SKTableMode.EHandle)
                {
                    //EHandleは7行固定
                    if (eventBuffer.Count == 7)
                    {
                        response = new EHANDLE(string.Join("\r\n", eventBuffer));
                        isResponseBodyReceived = true;
                        if (isResponseBodyReceived && isResponseCommandEndReceived)
                        {
                            TaskCompletionSource.TrySetResult(response);
                        }
                    }
                }
                else
                {
                    //その他は最大行数が不明なので、0.5秒後に終わらせる
                    Task.Delay(500).ContinueWith((t) =>
                    {
                        switch (Mode)
                        {
                            case SKTableMode.EAddr:
                                response = new EADDR(string.Join("\r\n", eventBuffer));
                                break;
                            case SKTableMode.ENeighbor:
                                response = new ENEIGHBOR(string.Join("\r\n", eventBuffer));
                                break;
                            case SKTableMode.ENbr:
                                response = new ENBR(string.Join("\r\n", eventBuffer));
                                break;
                            case SKTableMode.ESec:
                                response = new ESEC(string.Join("\r\n", eventBuffer));
                                break;
                            default:
                                break;
                        }
                        isResponseBodyReceived = true;
                        if (isResponseBodyReceived && isResponseCommandEndReceived)
                        {
                            TaskCompletionSource.TrySetResult(response);
                        }
                    });
                }
            }
        }
        internal override byte[] Arguments
        {
            get
            {
                return [(byte)(char)Mode];
            }
        }
    }
}
