namespace SkstackIpDotNet.Responses
{

    /// <summary>
    /// SKLL64コマンドの出力
    /// </summary>
    public class SKLL64 : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public SKLL64(string response) : base(response)
        {
            Ipaddr = response;
        }

        /// <summary>
        /// IPv6 リンクローカルアドレスが出力されます。
        /// </summary>
        public string Ipaddr { get; set; }
    }
}
