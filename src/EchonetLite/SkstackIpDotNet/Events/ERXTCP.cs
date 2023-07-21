namespace SkstackIpDotNet.Events
{
    /// <summary>
    /// TCP でデータを受信すると通知されます。
    /// </summary>
    public class ERXTCP : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ERXTCP(string response) : base(response)
        {
            var values = response.Split(' ');
            Sender = values[1];
            RPort = values[2];
            LPort = values[3];
            DataLen = values[4];
            Data = values[5];
        }

        /// <summary>
        /// 送信元 IPv6 アドレス
        /// </summary>
        public string Sender { get; set; }
        /// <summary>
        /// 送信元ポート番号
        /// </summary>
        public string RPort { get; set; }
        /// <summary>
        /// 送信先ポート番号
        /// </summary>
        public string LPort { get; set; }
        /// <summary>
        /// 受信したデータの長さ
        /// </summary>
        public string DataLen { get; set; }
        /// <summary>
        /// 受信データ
        /// </summary>
        public string Data { get; set; }
    }
}
