using System;
using System.Linq;
using Newtonsoft.Json;

namespace piSensorNet.Web.Models.DataTables
{
    public sealed class DataTablesColumnDefinitionJsonModel
    {
        /// <summary>
        /// Search definition
        /// </summary>
        [JsonProperty("search")]
        public DataTablesSearchDefinitionJsonModel SearchDefinition { get; set; } = new DataTablesSearchDefinitionJsonModel();

        [JsonProperty("orderable")]
        public bool IsSortable { get; set; }

        [JsonProperty("searchable")]
        public bool IsSearchable { get; set; }

        /// <summary>
        /// Data property name
        /// </summary>
        [JsonProperty("data")]
        public string PropertyName { get; set; }

        /// <summary>
        /// Property title
        /// </summary>
        [JsonProperty("name")]
        public string PropertyTitle { get; set; }
    }
}