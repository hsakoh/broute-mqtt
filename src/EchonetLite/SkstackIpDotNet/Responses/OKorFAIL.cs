namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// OKorFAILレスポンス
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public abstract class OKorFAIL(string response) : ReceiveData(response)
    {
    }
    /// <summary>
    /// OKレスポンス
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public class OK(string response) : OKorFAIL(response)
    {
    }
    /// <summary>
    /// FAILレスポンス
    /// </summary>
    /// <remarks>
    /// コンストラクタ
    /// </remarks>
    /// <param name="response"></param>
    public class FAIL(string response) : OKorFAIL(response)
    {
    }
}
