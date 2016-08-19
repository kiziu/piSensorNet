using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace piSensorNet.Web.Models.DataTables
{
    /// <summary>
    /// Class that encapsulates parameters sent to DataTables plugin
    /// </summary>
    public sealed class DataTablesJsonResultModel<T>
    {
        [JsonProperty("draw")]
        public int Echo { get; set; }

        /// <summary>
        /// Total items in the data source (despite filtration and pagination).
        /// </summary>
        [JsonProperty("recordsTotal")]
        public int TotalItems { get; set; }

        /// <summary>
        /// Total items after filtration.
        /// </summary>
        [JsonProperty("recordsFiltered")]
        public int TotalDisplayRecords { get; set; }

        [JsonProperty("items")]
        public IReadOnlyList<T> Items { get; set; }

        public DataTablesJsonResultModel(int echo, int totalItems, int totalDisplayRecords, IReadOnlyList<T> items)
        {
            Items = items;
            TotalDisplayRecords = totalDisplayRecords;
            TotalItems = totalItems;
            Echo = echo;
        }
    }

    public static class DataTablesJsonResultModel
    {
        public static DataTablesJsonResultModel<T> Create<T>(int echo, int totalItems, int totalDisplayRecords, IReadOnlyList<T> items)
        {
            return new DataTablesJsonResultModel<T>(echo, totalItems, totalDisplayRecords, items);
        }
    }
}
