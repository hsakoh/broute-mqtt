using EchoDotNetLite.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Models
{
    /// <summary>
    /// ECHONET オブジェクト（EOJ）
    /// </summary>
    public struct EOJ:IEquatable<EOJ>
    {
        /// <summary>
        /// クラスグループコード
        /// </summary>
        [JsonIgnore]
        public byte ClassGroupCode { get; set; }
        [JsonProperty("ClassGroupCode")]
        public string _ClassGroupCode { get { return $"{ClassGroupCode:X2}"; } }
        /// <summary>
        /// クラスクラスコード
        /// </summary>
        [JsonIgnore]
        public byte ClassCode { get; set; }
        [JsonProperty("ClassCode")]
        public string _ClassCode { get { return $"{ClassCode:X2}"; } }
        /// <summary>
        /// インスタンスコード
        /// </summary>
        [JsonIgnore]
        public byte InstanceCode { get; set; }
        [JsonProperty("InstanceCode")]
        public string _InstanceCode { get { return $"{InstanceCode:X2}"; } }


        public bool Equals(EOJ other)
        {
            return ClassGroupCode == other.ClassGroupCode
                && ClassCode == other.ClassCode
                && InstanceCode == other.InstanceCode;
        }

        public override bool Equals(object other)
        {
            if (other is EOJ)
                return Equals((EOJ)other);
            return false;
        }

        public override int GetHashCode()
        {
            return ClassGroupCode.GetHashCode()
                ^ ClassCode.GetHashCode()
                ^ InstanceCode.GetHashCode();
        }

        public static bool operator ==(EOJ c1, EOJ c2)
        {
            return c1.ClassGroupCode == c2.ClassGroupCode
                && c1.ClassCode == c2.ClassCode
                && c1.InstanceCode == c2.InstanceCode;
        }
        public static bool operator !=(EOJ c1, EOJ c2)
        {
            return !(c1 == c2);
        }
    }
}
