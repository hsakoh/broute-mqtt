using Newtonsoft.Json;

namespace SkstackIpDotNet
{
    /// <summary>
    /// レスポンスの既定クラス
    /// </summary>
    public class ReceiveData
    {
        /// <summary>
        /// レスポンス平文
        /// </summary>
        [JsonIgnore]
        public string PlainResponse { get; set; }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        /// 
        public ReceiveData()
        {
            PlainResponse = null;
        }

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        /// <param name="response">レスポンス平文</param>
        /// 
        public ReceiveData(string response)
        {
            PlainResponse = response;
        }

        /// <summary>
        /// ToString
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
