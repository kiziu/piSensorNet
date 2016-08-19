using System;
using System.Linq;
using Newtonsoft.Json;
using piSensorNet.Common.Enums;

namespace piSensorNet.Web.Models.DataTables
{
    public sealed class DataTablesColumnSortingDefinitionJsonModel
    {
        [JsonProperty("column")]
        public int ColumnIndex { get; set; }

        [JsonProperty("dir")]
        public SortingDirectionEnum Direction { get; set; }
    }
}