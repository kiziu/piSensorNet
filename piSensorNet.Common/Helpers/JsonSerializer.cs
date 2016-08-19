using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

using _JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace piSensorNet.Common.Helpers
{
    public static class JsonSerializer
    {
        [Pure]
        public static string Serialize<T>(T obj)
        {
            var settings = new JsonSerializerSettings
                           {
                               Formatting = Formatting.Indented,
                           };
            
            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter)
                                    {
                                        QuoteChar = '\'',
                                        Formatting = settings.Formatting,
                                    })
            {
                var serializer = _JsonSerializer.CreateDefault(settings);

                serializer.Serialize(jsonWriter, obj);

                jsonWriter.Flush();

                var serialized = stringWriter.ToString();

                return serialized;
            }
        }
    }
}
