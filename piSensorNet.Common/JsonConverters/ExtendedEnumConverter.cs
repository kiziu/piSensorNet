using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace piSensorNet.Common.JsonConverters
{
    public sealed class ExtendedEnumConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            writer.WriteValue((int)value);
            writer.WritePropertyName("name");
            writer.WriteValue(value.ToString());
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = (string)reader.Value;

            int intvalue;
            if (int.TryParse(value, out intvalue))
                return Enum.ToObject(objectType, intvalue);

            var members = objectType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var member = members
                .Where(i => i.Name.Equals(value, StringComparison.InvariantCulture)
                            || String.Equals(value, i.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName, StringComparison.InvariantCulture))
                .Single();

            return member.GetValue(null);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsEnum;
        }
    }
}
