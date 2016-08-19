using System;
using System.Linq;
using Newtonsoft.Json;
using piSensorNet.Common.JsonConverters;

namespace piSensorNet.Common.Enums
{
    [JsonConverter(typeof(NamedEnumConverter))]
    public enum SortingDirectionEnum
    {
        Unknown = 0,

        [JsonProperty("asc")]
        Ascending,

        [JsonProperty("desc")]
        Descending
    }
}