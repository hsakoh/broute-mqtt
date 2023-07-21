namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKPING
    /// </summary>
    public class EPONG : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EPONG(string response) : base(response)
        {
            var resp = response.Split(' ');
            Sender = resp[1];
        }
        /// <summary>
        /// 送信元 IPv6 アドレス
        /// </summary>
        public string Sender { get; set; }
    }
}
