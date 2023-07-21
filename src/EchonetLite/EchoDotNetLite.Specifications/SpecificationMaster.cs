using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EchoDotNetLite.Specifications
{
    /// <summary>
    /// ECHONETオブジェクト詳細マスタ
    /// </summary>
    internal sealed class SpecificationMaster
    {
        /// <summary>
        /// シングルトンイスタンス
        /// </summary>
        private static SpecificationMaster _Instance;
        /// <summary>
        /// プライベートコンストラクタ
        /// </summary>
        private SpecificationMaster()
        {
        }

        /// <summary>
        /// インスタンス取得
        /// </summary>
        /// <returns></returns>
        public static SpecificationMaster GetInstance()
        {
            if (_Instance == null)
            {
                var filePath = Path.Combine(GetSpecificationMasterDataDirectory(), "SpecificationMaster.json");
                _Instance = JsonConvert.DeserializeObject<SpecificationMaster>(File.ReadAllText(filePath, new UTF8Encoding(false)));
            }
            return _Instance;
        }

        internal static string GetSpecificationMasterDataDirectory()
        {
            return Path.Combine(AppContext.BaseDirectory, "MasterData");
        }

        /// <summary>
        /// ECHONET Lite SPECIFICATIONのバージョン
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// APPENDIX ECHONET 機器オブジェクト詳細規定のリリース番号
        /// </summary>
        public string AppendixRelease { get; set; }
        /// <summary>
        /// プロファイルオブジェクト
        /// </summary>
        public List<EchoClassGroup> プロファイル { get; set; }
        /// <summary>
        /// 機器オブジェクト
        /// </summary>
        public List<EchoClassGroup> 機器 { get; set; }

    }
}
