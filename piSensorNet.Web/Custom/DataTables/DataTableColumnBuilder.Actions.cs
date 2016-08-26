using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc.Rendering;
using piSensorNet.Web.Custom.DataTables.Interfaces;

namespace piSensorNet.Web.Custom.DataTables
{
    public sealed class DataTableActionsColumnBuilder<TElement> : IDataTableObjectGenerator
    {
        private readonly IHtmlHelper _htmlHelper;
        private readonly IList<DataTableActionColumnBuilder<TElement>> _columns = new List<DataTableActionColumnBuilder<TElement>>();

        public DataTableActionsColumnBuilder(IHtmlHelper htmlHelper)
        {
            _htmlHelper = htmlHelper;
        }

        public DataTableActionColumnBuilder<TElement> Name([NotNull] string name)
        {
            var columnBuilder = new DataTableActionColumnBuilder<TElement>(_htmlHelper, name);

            _columns.Add(columnBuilder);

            return columnBuilder;
        }

        public object ToObject()
            => (Expression<Func<Nested>>)(() =>
                DataTable.piSensorNet["DataTables"]["actions"].Invoke(_columns.ToDictionary(i => i.ActionName, i => i.ToObject())));
    }
}