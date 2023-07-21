using System.Collections.Generic;

namespace EchoDotNetLite.Specifications
{
    internal class PropertyMaster
    {
        /// <summary>
        /// ECHONET Lite SPECIFICATIONのバージョン
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// APPENDIX ECHONET 機器オブジェクト詳細規定のリリース番号
        /// </summary>
        public string AppendixRelease { get; set; }
        /// <summary>
        /// プロパティのリスト
        /// </summary>
        public List<EchoProperty> Properties { get; set; }
    }
}
