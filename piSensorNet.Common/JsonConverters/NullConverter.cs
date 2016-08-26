using System;
using System.Linq;
using Newtonsoft.Json;
using piSensorNet.Common.Custom;
using piSensorNet.Common.System;

namespace piSensorNet.Common.JsonConverters
{
    public sealed class NullConverter : JsonConverter
    {
        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) 
            => writer.WriteNull();

        public override bool CanConvert(Type objectType)
            => objectType == Reflector.Instance<Null>.Type;
    }
}
