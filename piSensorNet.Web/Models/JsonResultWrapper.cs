using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace piSensorNet.Web.Models
{
    public sealed class JsonResultWrapper<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("errors")]
        public IReadOnlyList<string> Errors { get; }

        [JsonProperty("data")]
        public T Data { get; }

        public JsonResultWrapper([NotNull] IReadOnlyList<string> errors)
        {
            if (errors == null)
                throw new ArgumentNullException(nameof(errors));

            if (errors.Count == 0)
                throw new ArgumentException("Empty", nameof(errors));

            Errors = errors;
            Success = false;
        }

        public JsonResultWrapper(T data)
        {
            Data = data;
            Success = true;
        }
    }
}
