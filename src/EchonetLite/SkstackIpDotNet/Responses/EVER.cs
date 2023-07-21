namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// SKVERのレスポンス
    /// </summary>
    public class EVER : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public EVER(string response) : base(response)
        {
            var resp = response.Split(' ');
            Version = resp[1];
        }
        /// <summary>
        /// x.x.x 形式のバージョン番号が ASCII 文字で出力されます。
        /// </summary>
        public string Version { get; set; }
    }
}
