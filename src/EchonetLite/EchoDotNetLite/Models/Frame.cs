using EchoDotNetLite.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// ECHONET Liteフレーム
    /// </summary>
    public class Frame
    {
        /// <summary>
        /// ECHONET Lite電文ヘッダー１(1B)
        /// ECHONETのプロトコル種別を指定する。
        /// </summary>
        [JsonIgnore]
        public EHD1 EHD1;
        [JsonProperty("EHD1")]
        public string _EHD1 { get { return $"{(byte)EHD1:X2}"; } }
        /// <summary>
        /// ECHONET Lite電文ヘッダー２(1B)
        /// EDATA部の電文形式を指定する。
        /// </summary>
        [JsonIgnore]
        public EHD2 EHD2;
        [JsonProperty("EHD2")]
        public string _EHD2 { get { return $"{(byte)EHD2:X2}"; } }
        /// <summary>
        /// トランザクションID(2B)
        /// </summary>
        [JsonIgnore]
        public ushort TID;
        [JsonProperty("TID")]
        public string _TID { get { return $"{BytesConvert.ToHexString(BitConverter.GetBytes(TID))}"; } }
        /// <summary>
        /// ECHONET Liteデータ
        /// ECHONET Lite 通信ミドルウェアにてやり取りされる電文のデータ領域。
        /// </summary>
        public IEDATA EDATA;
    }
}
