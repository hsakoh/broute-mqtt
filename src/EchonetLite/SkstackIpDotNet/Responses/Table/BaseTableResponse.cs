namespace SkstackIpDotNet.Responses.Table
{
    /// <summary>
    /// TABLEコマンドのレスポンス基底クラス
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public class BaseTableResponse(string response) : ReceiveData(response)
    {
    }
}
