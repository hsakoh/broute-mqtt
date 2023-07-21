namespace SkstackIpDotNet.Responses
{

    /// <summary>
    /// SKSREGのレスポンスクラス
    /// </summary>
    public class ESREG : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public ESREG(string response) : base(response)
        {
            var resp = response.Split(' ');
            Value = resp[1];
        }
        /// <summary>
        /// レジスタの現在値
        /// </summary>
        public string Value { get; set; }
    }
}
