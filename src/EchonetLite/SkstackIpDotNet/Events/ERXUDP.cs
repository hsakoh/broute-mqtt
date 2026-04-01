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
        public ERXUDP(string response, bool isBP35C0) : base(response)
        {
            var values = response.Split(' ');
            if (isBP35C0)
            {
                if (values.Length == 10)
                {
                    Sender = values[1];
                    Dest = values[2];
                    RPort = values[3];
                    LPort = values[4];
                    SenderLla = values[5];
                    Secured = values[6];
                    Side = values[7];
                    DataLen = values[8];
                    Data = values[9];
                }
                else if (values.Length == 11)
                {
                    Sender = values[1];
                    Dest = values[2];
                    RPort = values[3];
                    LPort = values[4];
                    SenderLla = values[5];
                    RSSI = values[6];
                    Secured = values[7];
                    Side = values[8];
                    DataLen = values[9];
                    Data = values[10];
                }
            }
            else
            {
                Sender = values[1];
                Dest = values[2];
                RPort = values[3];
                LPort = values[4];
                SenderLla = values[5];
                Secured = values[6];
                DataLen = values[7];
                Data = values[8];
            }
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
        /// 受信した UDP を構成する最後の MAC フレームの受信 RSSI レベル（SA2 レジスタ=1 の場合に表示されます）
        /// </summary>
        public string RSSI { get; set; }
        /// <summary>
        /// 1:受信した IP パケットを構成する MAC フレームが暗号化されていた場合
        /// 0: 受信した IP パケットを構成する MAC フレームが暗号化されていなかった場合
        /// </summary>
        public string Secured { get; set; }
        /// <summary>
        /// 受信した MAC 面 (0 or 1)
        /// </summary>
        public string Side { get; set; }
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
