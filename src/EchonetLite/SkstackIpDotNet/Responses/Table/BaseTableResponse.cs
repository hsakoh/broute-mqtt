namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// TABLEコマンドのレスポンス基底クラス
    /// </summary>
    public class BaseTableResponse : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public BaseTableResponse(string response) : base(response)
        {
        }
    }
}
