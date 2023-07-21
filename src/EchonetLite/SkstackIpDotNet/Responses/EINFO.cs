namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKINFOのレスポンス
    /// </summary>
    public class EINFO : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EINFO(string response) : base(response)
        {
            var resp = response.Split(' ');
            IPAddress = resp[1];
            Addr64 = resp[2];
            Channel = resp[3];
            PanId = resp[4];
            Addr16 = resp[5];
        }

        /// <summary>
        /// 端末に設定されているリンクローカルアドレスを表示します
        /// </summary>
        public string IPAddress { get; set; }
        /// <summary>
        /// 端末の IEEE 64bit MAC アドレスを表示します。
        /// </summary>
        public string Addr64 { get; set; }
        /// <summary>
        /// 現在使用している周波数の論理チャンネル番号を表示します。
        /// </summary>
        public string Channel { get; set; }
        /// <summary>
        /// 現在の PAN ID を表示します。
        /// </summary>
        public string PanId { get; set; }
        /// <summary>
        /// 現在のショートアドレスを表示します。
        /// </summary>
        public string Addr16 { get; set; }
    }
}
