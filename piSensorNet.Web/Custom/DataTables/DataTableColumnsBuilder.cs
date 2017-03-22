using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.Rendering;
using piSensorNet.Common.Extensions;
using piSensorNet.Web.Custom.DataTables.Interfaces;

namespace piSensorNet.Web.Custom.DataTables
{
    public sealed class DataTableColumnsBuilder<TElement> : IDataTableApplicable
    {
        private readonly IHtmlHelper _htmlHelper;
        private readonly IList<IDataTableObjectGenerator> _columns = new List<IDataTableObjectGenerator>();
        private readonly List<string> _names = new List<string>();

        public DataTableColumnsBuilder(IHtmlHelper htmlHelper)
        {
            _htmlHelper = htmlHelper;
        }

        public DataTableColumnBuilder<TElement, TValue> For<TValue>(Expression<Func<TElement, TValue>> expression)
            => new DataTableColumnBuilder<TElement, TValue>(_htmlHelper, expression).For(_columns.Add).AddMemberTo(_names);

        public DataTableActionsColumnBuilder<TElement> Actions(Action<DataTableActionsColumnBuilder<TElement>> builderAction)
        {
            var columnsBuilder = new DataTableActionsColumnBuilder<TElement>(_htmlHelper).For(_columns.Add);
            
            builderAction(columnsBuilder);

            return columnsBuilder;
        }

        public List<string> GetColumns()
            => _names;

        public void AddTo(IDictionary<DataTablePropertyEnum, object> dataTable)
            => dataTable.Add(DataTablePropertyEnum.columns, _columns.Map(i => i.ToObject()));
    }
}