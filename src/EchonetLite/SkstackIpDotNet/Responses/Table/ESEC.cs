using System;
using System.Collections.Generic;
using System.Text;

namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKTABLEのレスポンス
    /// MAC セキィリティのキー設定表示
    /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
    /// </summary>
    public class ESEC : BaseTableResponse
    {
        /// <summary>
        /// コンストラクタ 
        /// </summary>
        /// <param name="response"></param>
        public ESEC(string response) : base(response)
        {
        }
        //TODO 未実装
    }
}
