using System;
using System.Collections.Generic;
using System.Text;

namespace SkstackIpDotNet.Responses
{

    /// <summary>
    /// SKTABLEのレスポンス
    /// ネイバーキャッシュ
    /// </summary>
    public class ENEIGHBOR : BaseTableResponse
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ENEIGHBOR(string response) : base(response)
        {
            var rows = response.Split("\r\n");
            for (int i = 1; i < rows.Length; i++)
            {
                var cols = rows[i].Split(' ');
                List.Add(new NaighborCache()
                {
                    IPADDR = cols[0],
                    LLA = cols[1],
                    // TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
                    SHORTADDR = cols[2]
                });
            }
        }
        /// <summary>
        /// 自端末のネイバーキャッシュ内の IPv6 エントリー一覧
        /// </summary>
        public List<NaighborCache> List = new List<NaighborCache>();

        /// <summary>
        /// 自端末のネイバーキャッシュ内の IPv6 エントリー
        /// </summary>
        public class NaighborCache
        {
            /// <summary>
            /// ネイバーキャッシュに登録されている IPv6 アドレス（グローバル、リンクローカル両方を含む全て）
            /// </summary>
            public string IPADDR { get; set; }
            /// <summary>
            /// IPADDRに対応するリンク層 64bit アドレス
            /// SHORTADDRが 0xFFFF の場合、このフィールドにはIPADDRに対応する 64bit アドレスが表示されます。
            /// SHORTADDRが 0xFFFF 以外の場合、このフィールドの内容は不定になります。
            /// </summary>
            public string LLA { get; set; }
            /// <summary>
            /// IPADDRに対応するリンク層 16bit アドレス
            /// 対応する 16bit アドレスがその時点で未確定の場合、0xFFFF が表示されます。
            /// TODO SKSTACK-IP(Single-hop Edition)に記述が無いが、応答するのでそのままとする
            /// </summary>
            public string SHORTADDR { get; set; }

        }
    }
}
