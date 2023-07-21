using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// クラスグループ
    /// </summary>
    public class EchoClassGroup
    {
        /// <summary>
        /// クラスグループコード
        /// </summary>
        [JsonConverter(typeof(SingleByteHexStringJsonConverter))]
        public byte ClassGroupCode { get; set; }
        /// <summary>
        /// クラスグループ名
        /// </summary>
        public string ClassGroupNameOfficial { get; set; }
        /// <summary>
        /// C#での命名に使用可能なクラスグループ名
        /// </summary>
        public string ClassGroupName { get; set; }
        /// <summary>
        /// スーパークラス ない場合NULL
        /// </summary>
        public string SuperClass { get; set; }
        /// <summary>
        /// クラスグループに属するクラスのリスト
        /// </summary>
        public List<EchoClass> ClassList { get; set; }
    }
}
