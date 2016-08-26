using System;
using System.Linq;

namespace piSensorNet.Web.Custom.DataTables.Interfaces
{
    internal interface IDataTableObjectGenerator
    {
        object ToObject();
    }
}