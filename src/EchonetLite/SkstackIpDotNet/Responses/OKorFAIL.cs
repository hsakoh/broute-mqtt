namespace SkstackIpDotNet.Responses
{
    /// <summary>
    /// OKorFAILレスポンス
    /// </summary>
    public abstract class OKorFAIL : ReceiveData
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public OKorFAIL(string response) : base(response)
        {
        }
    }
    /// <summary>
    /// OKレスポンス
    /// </summary>
    public class OK : OKorFAIL
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public OK(string response) : base(response)
        {
        }
    }
    /// <summary>
    /// FAILレスポンス
    /// </summary>
    public class FAIL : OKorFAIL
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="response"></param>
        public FAIL(string response) : base(response)
        {
        }
    }
}
