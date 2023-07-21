using System.Collections.Generic;

namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKSCANのレスポンス
    /// ED スキャンの実行結果を、RSSI 値で一覧表示します。
    /// </summary>
    public class EEDSCAN : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EEDSCAN(string response) : base(response)
        {
            var resp = response.Split(' ');
            for (int i = 0; i < resp.Length - 1; i += 2)
            {
                List.Add(new ChannelRssi()
                {
                    Channel = resp[i],
                    Rssi = resp[i + 1],
                });
            }
        }

        /// <summary>
        ///  ED スキャンの実行結果一覧
        /// </summary>
        public List<ChannelRssi> List = new List<ChannelRssi>();
        /// <summary>
        /// ED スキャンの実行結果
        /// </summary>
        public class ChannelRssi
        {
            /// <summary>
            /// 測定した周波数の論理チャンネル番号
            /// </summary>
            public string Channel { get; set; }
            /// <summary>
            /// 測定した RSSI 値 (RSSI – 107dBm))
            /// </summary>
            public string Rssi { get; set; }
        }
    }
}
