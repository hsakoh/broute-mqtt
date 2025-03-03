namespace SkstackIpDotNet.Responses
{

    /// <summary>
    /// SKLL64コマンドの出力
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public class SKLL64(string response) : ReceiveData(response)
    {

        /// <summary>
        /// IPv6 リンクローカルアドレスが出力されます。
        /// </summary>
        public string Ipaddr { get; set; } = response;
    }
}
