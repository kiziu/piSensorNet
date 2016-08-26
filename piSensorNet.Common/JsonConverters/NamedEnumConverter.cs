using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace piSensorNet.Common.JsonConverters
{
    public sealed class NamedEnumConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value.GetType();
            var member = type.GetField(value.ToString());
            var name = member.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? member.Name;

            writer.WriteValue(name);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = (string)reader.Value;
            var members = objectType.GetFields(BindingFlags.Public | BindingFlags.Static);
            var member = members
                .Where(i => i.Name.Equals(value, StringComparison.InvariantCulture)
                            || String.Equals(value, i.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName, StringComparison.InvariantCulture))
                .Single();

            return member.GetValue(null);
        }

        public override bool CanConvert(Type objectType) 
            => objectType.IsEnum;
    }
}