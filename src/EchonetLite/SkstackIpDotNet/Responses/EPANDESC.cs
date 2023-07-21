namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKSCANのレスポンス
    /// </summary>
    public class EPANDESC : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EPANDESC() : base()
        {
        }
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EPANDESC(string response) : base(response)
        {
            var resp = response.Split("\r\n");
            Channel = resp[1].Split(':')[1];
            ChannelPage = resp[2].Split(':')[1];
            PanID = resp[3].Split(':')[1];
            Addr = resp[4].Split(':')[1];
            LQI = resp[5].Split(':')[1];
            if (resp.Length > 6)
            {
                //IEなしの場合、行がなしとなる
                PairID = resp[6].Split(':')[1];
            }
        }

        /// <summary>
        /// 発見した PAN の周波数（論理チャンネル番号）
        /// </summary>
        public string Channel { get; set; }
        /// <summary>
        /// 発見した PAN のチャンネルページ
        /// </summary>
        public string ChannelPage { get; set; }
        /// <summary>
        /// 発見した PAN の PAN ID
        /// </summary>
        public string PanID { get; set; }
        /// <summary>
        /// アクティブスキャン応答元のアドレス
        /// </summary>
        public string Addr { get; set; }
        /// <summary>
        /// 受信したビーコンの受信 RSSI
        /// </summary>
        public string LQI { get; set; }
        /// <summary>
        /// （IE が含まれる場合）相手から受信した Pairing ID
        /// </summary>
        public string PairID { get; set; }
    }
}
