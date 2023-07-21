using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EchoDotNetLite.Specifications
{
    internal class EchonetObject : IEchonetObject
    {
        public EchonetObject(byte classGroupCode, byte classCode)
        {
            ClassGroup = SpecificationMaster.GetInstance().プロファイル.Where(p => p.ClassGroupCode == classGroupCode).FirstOrDefault();
            ClassGroup ??= SpecificationMaster.GetInstance().機器.Where(p => p.ClassGroupCode == classGroupCode).FirstOrDefault();
            if (ClassGroup != null)
            {
                //スーパークラスのプロパティを列挙
                
                var superClassFilePath = Path.Combine(SpecificationMaster.GetSpecificationMasterDataDirectory(), $"{ClassGroup.SuperClass}.json");
                if (File.Exists(superClassFilePath))
                {
                    var superClassProperties = JsonConvert.DeserializeObject<PropertyMaster>(File.ReadAllText(superClassFilePath, new UTF8Encoding(false)));
                    Properties.AddRange(superClassProperties.Properties);
                }
                Class = ClassGroup.ClassList.Where(c => c.Status && c.ClassCode == classCode).FirstOrDefault();
                if (Class.Status)
                {
                    var classFilePath = Path.Combine(SpecificationMaster.GetSpecificationMasterDataDirectory(),$"0x{ClassGroup.ClassGroupCode:X2}-{ClassGroup.ClassGroupName}", $"0x{Class.ClassCode:X2}-{Class.ClassName}.json");
                    if (File.Exists(classFilePath))
                    {
                        //クラスのプロパティを列挙
                        var classProperties = JsonConvert.DeserializeObject<PropertyMaster>(File.ReadAllText(classFilePath, new UTF8Encoding(false)));
                        Properties.AddRange(classProperties.Properties);
                    }
                }
            }
        }
        /// <summary>
        /// クラスグループコード
        /// </summary>
        public EchoClassGroup ClassGroup { get; set; }
        /// <summary>
        /// クラスコード
        /// </summary>
        public EchoClass Class { get; set; }

        /// <summary>
        /// 仕様上定義済みのプロパティの一覧
        /// </summary>
        internal List<EchoProperty> Properties { get; set; } = new List<EchoProperty>();

        /// <summary>
        /// 仕様上定義済みのGETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> GetProperties
        {
            get { return Properties.Where(p => p.Get); }
        }
        /// <summary>
        /// 仕様上定義済みのSETプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> SetProperties
        {
            get { return Properties.Where(p => p.Set); }
        }
        /// <summary>
        /// 仕様上定義済みのANNOプロパティの一覧
        /// </summary>
        public IEnumerable<EchoProperty> AnnoProperties
        {
            get { return Properties.Where(p => p.Anno); }
        }
    }
}
