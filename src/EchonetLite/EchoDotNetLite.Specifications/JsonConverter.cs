using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Specifications
{
    internal sealed class SingleByteHexStringJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(uint).Equals(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue($"0x{value:x}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (!(reader.Value is string str) || !str.StartsWith("0x"))
                throw new JsonSerializationException();
            return Convert.ToByte(str,16);
        }
    }
}
