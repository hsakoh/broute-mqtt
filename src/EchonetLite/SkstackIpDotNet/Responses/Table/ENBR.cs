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
    public class ENBR : BaseTableResponse
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ENBR(string response) : base(response)
        {
        }
        //TODO 未実装
    }
}
