using System;
using System.Collections.Generic;
using System.Text;

namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKTABLEのレスポンス
    /// 自端末で利用可能な IP アドレス一覧
    /// </summary>
    public class EADDR : BaseTableResponse
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EADDR(string response) : base(response)
        {
            var rows = response.Split("\r\n");
            for (int i = 1; i < rows.Length; i++)
            {
                List.Add(new Addr()
                {
                    IPADDR = rows[i]
                });
            }
        }
        /// <summary>
        /// 自端末で利用可能な IPv6 アドレス一覧
        /// </summary>
        public List<Addr> List = new List<Addr>();

        /// <summary>
        /// 自端末で利用可能な IPv6 アドレス
        /// </summary>
        public class Addr
        {
            /// <summary>
            /// IPv6 アドレス（グローバル、リンクローカル両方を含む全て）
            /// </summary>
            public string IPADDR { get; set; }

        }
    }
}
