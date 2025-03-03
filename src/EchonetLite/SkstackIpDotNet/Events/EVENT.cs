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
        public EVENT(string response, bool isBP35C0) : base(response)
        {
            var resp = response.Split(' ');
            Num = resp[1];
            Sender = resp[2];
            if (isBP35C0)
            {
                Side = resp[3];
                if (resp.Length > 4)
                {
                    Param = resp[4];
                }
            }
            else
            {
                if (resp.Length > 3)
                {
                    Param = resp[3];
                }
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
        /// スキャンを実行した MAC 面(0 または 1)
        /// </summary>
        public string Side { get; set; }
        /// <summary>
        /// イベント固有の引数
        /// </summary>
        public string Param { get; set; }
    }
}
