using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace piSensorNet.Web.Models.DataTables
{
    /// <summary>
    /// Class that encapsulates most common parameters sent by DataTables plugin
    /// </summary>
    public sealed class DataTablesQueryParametersJsonModel
    {
        /// <summary>
        /// Request sequence number sent by DataTable,
        /// same value must be returned in response
        /// </summary>       
        [JsonProperty("draw")]
        public int Echo { get; set; }

        /// <summary>
        /// Number of records that should be shown in table
        /// </summary>
        [JsonProperty("length")]
        public int ItemsPerPageCount { get; set; }

        /// <summary>
        /// First record that should be shown(used for paging)
        /// </summary>
        [JsonProperty("start")]
        public uint FirstItemNumber { get; set; }

        /// <summary>
        /// Column definitions
        /// </summary>
        [JsonProperty("columns")]
        public IReadOnlyList<DataTablesColumnDefinitionJsonModel> ColumnDefinitions { get; set; } = new List<DataTablesColumnDefinitionJsonModel>();

        /// <summary>
        /// Column sorting definitions
        /// </summary>
        [JsonProperty("order")]
        public IReadOnlyList<DataTablesColumnSortingDefinitionJsonModel> ColumnSortingDefinitions { get; set; } = new List<DataTablesColumnSortingDefinitionJsonModel>();

        /// <summary>
        /// Search definition
        /// </summary>
        [JsonProperty("search")]
        public DataTablesSearchDefinitionJsonModel SearchDefinition { get; set; } = new DataTablesSearchDefinitionJsonModel();

        [JsonIgnore]
        public int PageSize => Convert.ToInt32(Math.Max(ItemsPerPageCount, 1));

        [JsonIgnore]
        public int PageNumber => Convert.ToInt32(Math.Ceiling(Decimal.Divide(FirstItemNumber, PageSize)) + 1);
    }
}