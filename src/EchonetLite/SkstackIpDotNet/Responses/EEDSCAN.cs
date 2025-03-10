﻿using System.Collections.Generic;

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
        public EEDSCAN(string response, bool isBP35C0) : base(response)
        {
            var resp = response.Split(' ');
            if (isBP35C0)
            {
                Side = resp[0];
            }
            for (int i = isBP35C0 ? 1 : 0; i < resp.Length - 1; i += 2)
            {
                List.Add(new ChannelRssi()
                {
                    Channel = resp[i],
                    Rssi = resp[i + 1],
                });
            }
        }

        /// <summary>
        /// スキャンを実行した MAC 面(0 または 1)
        /// </summary>
        public string Side { get; set; }
        /// <summary>
        ///  ED スキャンの実行結果一覧
        /// </summary>
        public List<ChannelRssi> List = [];
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
