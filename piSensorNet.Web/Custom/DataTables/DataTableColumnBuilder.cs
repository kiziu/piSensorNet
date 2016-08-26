using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc.Rendering;
using piSensorNet.Common.Extensions;
using piSensorNet.Web.Custom.DataTables.Interfaces;

namespace piSensorNet.Web.Custom.DataTables
{
    public abstract class DataTableColumnBuilder
    {
        internal enum PropertyEnum
        {
            name,
            type,
            visible,
            render,
            data
        }
    }

    public sealed class DataTableColumnBuilder<TElement, TValue> : DataTableColumnBuilder, IDataTableObjectGenerator
    {
        internal readonly IHtmlHelper _htmlHelper;
        private readonly IDictionary<PropertyEnum, object> _properties = new Dictionary<PropertyEnum, object>();
        private readonly string _data;

        public DataTableColumnBuilder(IHtmlHelper htmlHelper, Expression<Func<TElement, TValue>> expression)
        {
            _htmlHelper = htmlHelper;

            var memberExpression = expression.ExtractMemberExpression();
            if (memberExpression == null)
                throw new ArgumentException($"Expression '{expression}' does not contain '{nameof(MemberExpression)}'.");

            _data = memberExpression.Member.Name;
            
            Name(_data);
            _properties.Add(PropertyEnum.data, _data);
        }

        internal DataTableColumnBuilder<TElement, TValue> AddMemberTo(IList<string> names)
        {
            names.Add(_data);

            return this;
        }

        public DataTableColumnBuilder<TElement, TValue> Name([NotNull] string name)
        {
            _properties.AddOrReplace(PropertyEnum.name, name);

            return this;
        }

        public DataTableColumnBuilder<TElement, TValue> Type(DataTableColumnTypeEnum type)
        {
            _properties.AddOrReplace(PropertyEnum.type, type);

            return this;
        }

        public DataTableColumnBuilder<TElement, TValue> Visible(bool visible)
        {
            _properties.AddOrReplace(PropertyEnum.visible, visible);

            return this;
        }

        internal DataTableColumnBuilder<TElement, TValue> Property(PropertyEnum property, object value)
        {
            _properties.AddOrReplace(property, value);

            return this;
        }

        public object ToObject()
            => _properties;
    }
}