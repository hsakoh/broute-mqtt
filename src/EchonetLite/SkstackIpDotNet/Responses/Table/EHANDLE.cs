using System;
using System.Collections.Generic;
using System.Text;

namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKTABLEのレスポンス
    /// TCP ハンドル状態一覧
    /// </summary>
    public class EHANDLE : BaseTableResponse
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EHANDLE(string response) : base(response)
        {
            var rows = response.Split("\r\n");
            for(int i = 1; i < rows.Length; i++)
            {
                var cols = rows[i].Split(' ');
                List.Add(new TcpHandleState()
                {
                    Handle = cols[0],
                    IPADDR = cols[1],
                    RPORT = cols[2],
                    LPORT = cols[3],
                });
            }
        }
        /// <summary>
        /// TCP ハンドル状態一覧
        /// </summary>
        public List<TcpHandleState> List = new List<TcpHandleState>();

        /// <summary>
        /// TCP ハンドル状態
        /// </summary>
        public class TcpHandleState
        {
            /// <summary>
            /// ハンドル番号 (1-6)
            /// </summary>
            public string Handle { get; set; }
            /// <summary>
            /// この TCP ハンドルの接続先 IP アドレス（コロン表記）
            /// </summary>
            public string IPADDR { get; set; }
            /// <summary>
            /// この TCP ハンドルの接続先ポート番号
            /// </summary>
            public string RPORT { get; set; }
            /// <summary>
            /// この TCP ハンドルの接続元ポート番号
            /// </summary>
            public string LPORT { get; set; }

        }
    }
}
