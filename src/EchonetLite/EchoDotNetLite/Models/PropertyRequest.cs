using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Models
{
    public class PropertyRequest
    {
        /// <summary>
        /// ECHONET Liteプロパティ(1B)
        /// </summary>
        [JsonIgnore]
        public byte EPC;
        [JsonProperty("EPC")]
        public string _EPC { get { return $"{EPC:X2}"; } }
        /// <summary>
        /// EDTのバイト数(1B)
        /// </summary>
        [JsonIgnore]
        public byte PDC;
        [JsonProperty("PDC")]
        public string _PDC { get { return $"{PDC:X2}"; } }
        /// <summary>
        /// プロパティ値データ(PDCで指定)
        /// </summary>
        [JsonIgnore]
        public byte[] EDT;
        [JsonProperty("EDT")]
        public string _EDT { get { return $"{BytesConvert.ToHexString(EDT)}"; } }
    }
}
