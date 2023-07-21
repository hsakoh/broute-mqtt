namespace SkstackIpDotNet.Events
{
    /// <summary>
    /// EVENT
    /// </summary>
    public class EVENT : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EVENT(string response) : base(response)
        {
            var resp = response.Split(' ');
            Num = resp[1];
            Sender = resp[2];
            if (resp.Length > 3)
            {
                Param = resp[3];
            }
        }
        /// <summary>
        /// イベント番号
        /// </summary>
        public string Num { get; set; }
        /// <summary>
        /// イベントのトリガーとなったメッセージの発信元アドレス
        /// </summary>
        public string Sender { get; set; }
        /// <summary>
        /// イベント固有の引数
        /// </summary>
        public string Param { get; set; }
    }
}
