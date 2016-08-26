using System;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using piSensorNet.Common.Custom;

namespace piSensorNet.Web.Custom.DataTables
{
    public static class DataTableColumnBuilderExtensions
    {
        [NotNull]
        public static DataTableColumnBuilder<TElement, TValue> Localize<TElement, TValue>([NotNull] this DataTableColumnBuilder<TElement, TValue> builder, [NotNull] Expression<Func<JsonLiteral>> expression)
            where TValue : struct
            => builder.Property(DataTableColumnBuilder.PropertyEnum.render, expression);

        [NotNull]
        public static DataTableColumnBuilder<TElement, DateTime> Format<TElement>([NotNull] this DataTableColumnBuilder<TElement, DateTime> builder, [NotNull] Expression<Func<JsonLiteral>> expression)
            => builder.Property(DataTableColumnBuilder.PropertyEnum.render, expression);
    }
}