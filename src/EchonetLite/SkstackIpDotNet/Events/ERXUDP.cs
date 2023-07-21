namespace SkstackIpDotNet.Events
{
    /// <summary>
    /// 自端末宛ての UDP（マルチキャスト含む）を受信すると通知されます。
    /// </summary>
    public class ERXUDP : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ERXUDP(string response) : base(response)
        {
            var values = response.Split(' ');
            Sender = values[1];
            Dest = values[2];
            RPort = values[3];
            LPort = values[4];
            SenderLla = values[5];
            Secured = values[6];
            DataLen = values[7];
            Data = values[8];
        }

        /// <summary>
        /// 送信元 IPv6 アドレス
        /// </summary>
        public string Sender { get; set; }
        /// <summary>
        /// 送信先 IPv6 アドレス
        /// </summary>
        public string Dest { get; set; }
        /// <summary>
        /// 送信元ポート番号
        /// </summary>
        public string RPort { get; set; }
        /// <summary>
        /// 送信先ポート番号
        /// </summary>
        public string LPort { get; set; }
        /// <summary>
        /// 送信元の MAC 層アドレス(64bit)
        /// </summary>
        public string SenderLla { get; set; }
        /// <summary>
        /// 1:受信した IP パケットを構成する MAC フレームが暗号化されていた場合
        /// 0: 受信した IP パケットを構成する MAC フレームが暗号化されていなかった場合
        /// </summary>
        public string Secured { get; set; }
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
