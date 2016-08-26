using System;
using System.Linq;
using piSensorNet.Web.Models.Base;

namespace piSensorNet.Web.Custom.DataTables
{
    public static class VariousDataTablesExtensions
    {
        public static TModel[] AsArray<TModel>(this TModel item)
            where TModel : BaseModel
        {
            throw new NotImplementedException();
        }

        public static TEnum Value<TEnum>(this TEnum item)
            where TEnum : struct
        {
            throw new NotImplementedException();
        }
    }
}
