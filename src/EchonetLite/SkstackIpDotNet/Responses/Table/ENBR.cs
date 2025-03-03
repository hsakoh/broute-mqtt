using System;
using System.Collections.Generic;
using System.Text;

namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKTABLEのレスポンス
    /// ネイバーテーブル一覧
    /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public class ENBR(string response) : BaseTableResponse(response)
    {
        //TODO 未実装
    }
}
