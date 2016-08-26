using System;
using System.Linq;
using System.Collections.Generic;

namespace piSensorNet.Web.Custom.DataTables.Interfaces
{
    internal interface IDataTableApplicable
    {
        void AddTo(IDictionary<DataTablePropertyEnum, object> dataTable);
    }
}