using System;
using System.Linq;
using Newtonsoft.Json;

namespace piSensorNet.Web.Models.DataTables
{
    public sealed class DataTablesSearchDefinitionJsonModel
    {
        /// <summary>
        /// Text used for filtering
        /// </summary>
        [JsonProperty("value")]
        public string SearchedText { get; set; }

        /// <summary>
        /// Is Regex used
        /// </summary>
        [JsonProperty("regex")]
        public bool IsRegexUsed { get; set; }
    }
}