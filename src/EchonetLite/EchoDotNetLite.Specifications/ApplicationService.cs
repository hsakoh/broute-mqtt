using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace EchoDotNetLite.Specifications
{

    /// <summary>
    /// アプリケーションサービス
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ApplicationService
    {
        /// <summary>
        /// (Mobile services)○M
        /// </summary>
        モバイルサービス,
        /// <summary>
        /// (Energy services)○E
        /// </summary>
        エネルギーサービス,
        /// <summary>
        /// (Home amenity services)○Ha
        /// </summary>
        快適生活支援サービス,
        /// <summary>
        /// (Home health-care services)○Hh
        /// </summary>
        ホームヘルスケアサービス,
        /// <summary>
        /// (Security services)○S
        /// </summary>
        セキュリティサービス,
        /// <summary>
        /// (Remote appliance maintenance services)○R
        /// </summary>
        機器リモートメンテナンスサービス,
    }
}
