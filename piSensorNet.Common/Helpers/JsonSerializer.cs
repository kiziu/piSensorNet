using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using piSensorNet.Common.Extensions;
using JetBrains.Annotations;
using _JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace piSensorNet.Common.Helpers
{
    public static class JsonSerializer
    {
        [Pure]
        public static string Serialize<T>(T obj, [CanBeNull] IReadOnlyCollection<JsonConverter> converters = null)
        {
            var settings = new JsonSerializerSettings
                           {
                               Formatting = Formatting.Indented,
                           };

            converters?.For(settings.Converters.Add);

            var serializer = _JsonSerializer.CreateDefault(settings);

            using (var stringWriter = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(stringWriter)
                                    {
                                        QuoteChar = '\'',
                                        Formatting = serializer.Formatting,
                                        IndentChar = '\t',
                                        Indentation = 1,
                                    })
            {
                serializer.Serialize(jsonWriter, obj);

                jsonWriter.Flush();

                var serialized = stringWriter.ToString();

                return serialized;
            }
        }
    }
}