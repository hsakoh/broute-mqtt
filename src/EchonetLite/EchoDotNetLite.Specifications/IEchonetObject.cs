using System.Collections.Generic;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// ECHONET Lite オブジェクト
    /// </summary>
    public interface IEchonetObject
    {
        /// <summary>
        /// クラスグループ情報
        /// クラスグループコード
        /// </summary>
        EchoClassGroup ClassGroup { get; set; }
        /// <summary>
        /// クラス情報
        /// クラスコード
        /// </summary>
        EchoClass Class { get; set; }
        /// <summary>
        /// 仕様上定義済みのGETプロパティの一覧
        /// </summary>
        IEnumerable<EchoProperty> GetProperties { get; }
        /// <summary>
        /// 仕様上定義済みのSETプロパティの一覧
        /// </summary>
        IEnumerable<EchoProperty> SetProperties { get; }
        /// <summary>
        /// 仕様上定義済みのANNOプロパティの一覧
        /// </summary>
        IEnumerable<EchoProperty> AnnoProperties { get; }
    }
}