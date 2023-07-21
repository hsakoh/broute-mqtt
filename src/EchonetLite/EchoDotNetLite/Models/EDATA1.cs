using EchoDotNetLite.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using static EchoDotNetLite.Models.Frame;

namespace EchoDotNetLite.Models
{

    /// <summary>
    /// 電文形式 1（規定電文形式）
    /// </summary>
    public class EDATA1 : IEDATA
    {
        /// <summary>
        /// 送信元ECHONET Liteオブジェクト指定(3B)
        /// </summary>
        public EOJ SEOJ { get; set; }
        /// <summary>
        /// 相手先ECHONET Liteオブジェクト指定(3B)
        /// </summary>
        public EOJ DEOJ { get; set; }
        /// <summary>
        /// ECHONET Liteサービス(1B)
        /// ECHONET Liteサービスコード
        /// </summary>
        [JsonIgnore]
        public ESV ESV { get; set; }
        [JsonProperty("ESV")]
        public string _ESV { get { return $"{(byte)ESV:X2}"; } }

        public List<PropertyRequest> OPCList { get; set; }
        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// のみ使用
        /// </summary>
        public List<PropertyRequest> OPCGetList { get; set; }
        /// <summary>
        /// ４.２.３.４ プロパティ値書き込み読み出しサービス［0x6E,0x7E,0x5E］
        /// のみ使用
        /// </summary>
        public List<PropertyRequest> OPCSetList { get; set; }
    }
}
