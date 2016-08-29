using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using piSensorNet.Common.Enums;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.JsonConverters;
using piSensorNet.Web.Custom.DataTables.Interfaces;
using JsonSerializer = piSensorNet.Common.Helpers.JsonSerializer;

namespace piSensorNet.Web.Custom.DataTables
{
    public abstract class DataTable
    {
        public static readonly ICollection<string> Fields = new List<string>
                                                            {
                                                                $"{nameof(DataTable)}.{nameof(JSON)}",
                                                                $"{nameof(DataTable)}.{nameof(piSensorNet)}",
                                                            };

        // ReSharper disable once UnassignedReadonlyField
        protected internal static readonly Nested JSON;

        // ReSharper disable once UnassignedReadonlyField
        // ReSharper disable once InconsistentNaming
        protected internal static readonly Nested piSensorNet;
        
        protected internal static readonly string[] JsonLineSeparator = {"\r\n"};

        protected internal static readonly Lazy<IReadOnlyCollection<JsonConverter>> JsonConverters
            = new Lazy<IReadOnlyCollection<JsonConverter>>(() =>
                new JsonConverter[]
                {
                    new ExpressionFuncConverter(Fields.MapArray(i => i)),
                    new StringEnumConverter(),
                    new JsonLiteralConverter()
                });

        #region Sources

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Local")]
        protected sealed class AjaxUrlSource : IDataTableApplicable
        {
            private readonly IUrlHelper _urlHelper;

            public string Action { get; }
            public string Controller { get; }

            public AjaxUrlSource([NotNull] IHtmlHelper htmlHelper, [NotNull] string action, [CanBeNull] string controller)
            {
                _urlHelper = htmlHelper.ViewContext.HttpContext.RequestServices.GetService<IUrlHelper>();

                Action = action;
                Controller = controller;
            }

            public void AddTo(IDictionary<DataTablePropertyEnum, object> dataTable)
                => dataTable.Add(DataTablePropertyEnum.ajax, new
                                                             {
                                                                 url = _urlHelper.Action(Action, Controller),
                                                                 contentType = "application/json",
                                                                 dataType = "json",
                                                                 data = (Expression<Func<object, object, string>>)
                                                                     ((oData, oSettings) => JSON["stringify"].Invoke(oData))
                                                             });
        }

        #endregion
    }

    public sealed class DataTable<TElement> : DataTable, IHtmlContent
    {
        private readonly IHtmlHelper _htmlHelper;
        private readonly string _tableID;

        private int _tabNum;
        private bool _indentFirstLine;

        private IDataTableApplicable _source;

        private readonly IDictionary<DataTablePropertyEnum, object> _data =
            new Dictionary<DataTablePropertyEnum, object>
            {
                {DataTablePropertyEnum.searching, false},
                {DataTablePropertyEnum.paging, false},
            };

        private DataTableColumnsBuilder<TElement> _columnBuilder;

        private Expression<Func<TElement, object>> _defaultSortingProperty;
        private SortingDirectionEnum _defaultSortingDirection;

        public DataTable([NotNull] IHtmlHelper htmlHelper, [NotNull] string tableID)
        {
            _htmlHelper = htmlHelper;
            _tableID = tableID;
        }

        [NotNull]
        public DataTable<TElement> Indent(int tabNum, bool indentFirstLine = false)
        {
            _tabNum = tabNum;
            _indentFirstLine = indentFirstLine;

            return this;
        }

        [NotNull]
        public DataTable<TElement> AjaxSource([AspMvcAction] [NotNull] string action, [AspMvcController] [NotNull] string controller)
            => AjaxSource(new AjaxUrlSource(_htmlHelper, action, controller));

        [NotNull]
        public DataTable<TElement> AjaxSource([AspMvcAction] [NotNull] string action)
            => AjaxSource(new AjaxUrlSource(_htmlHelper, action, null));

        [NotNull]
        public DataTable<TElement> Title([NotNull] string title)
            => Property(DataTablePropertyEnum.title, title);

        [NotNull]
        public DataTable<TElement> Searching(bool searching)
            => Property(DataTablePropertyEnum.searching, searching);

        [NotNull]
        public DataTable<TElement> Paging(bool paging)
            => Property(DataTablePropertyEnum.paging, paging);

        [NotNull]
        public DataTable<TElement> DefaultSorting([NotNull] Expression<Func<TElement, object>> property, SortingDirectionEnum direction)
        {
            _defaultSortingProperty = property;
            _defaultSortingDirection = direction;

            return this;
        }

        [NotNull]
        public DataTable<TElement> Columns(Action<DataTableColumnsBuilder<TElement>> columnCreator)
        {
            _columnBuilder = new DataTableColumnsBuilder<TElement>(_htmlHelper);

            columnCreator(_columnBuilder);

            return this;
        }

        #region Helpers

        [NotNull]
        private DataTable<TElement> Property(DataTablePropertyEnum property, [NotNull] object value)
        {
            _data.AddOrReplace(property, value);

            return this;
        }

        [NotNull]
        private DataTable<TElement> AjaxSource(IDataTableApplicable source)
        {
            _source = source;

            return this;
        }

        private void Prepare()
        {
            _source.AddTo(_data);

            if (_defaultSortingProperty != null)
            {
                var name = _defaultSortingProperty.ExtractMemberExpression().Member.Name;

                var columnNames = _columnBuilder.GetColumns();
                var index = columnNames.IndexOf(name);

                if (index <= 0)
                    throw new Exception($"Column '{name}' cannot be used for sorting - it's not present in the column list.");

                _data.Add(DataTablePropertyEnum.sorting, new object[] {new object[] {index, _defaultSortingDirection}});
            }

            _columnBuilder.AddTo(_data);
        }

        #endregion

        public string Render()
        {
            Prepare();

            var builder = new StringBuilder();
            var indentation = new string('\t', _tabNum + 1); // inner has to be indented one tab more

            if (_indentFirstLine)
                builder.Append('\t', _tabNum);

            builder.AppendLine($"$('table#{_tableID}')");
            builder.Append(indentation).Append(".dataTable(");

            JsonSerializer.Serialize(_data, JsonConverters.Value)
                          .Split(JsonLineSeparator, StringSplitOptions.None)
                          .Each((i, e) =>
                                {
                                    if (i > 0)
                                        builder.Append(indentation);

                                    builder.AppendLine(e);
                                });

            builder.Length -= Environment.NewLine.Length;
            builder.AppendLine(");");

            return builder.ToString();
        }

        #region Overrides

        public void WriteTo(TextWriter writer, IHtmlEncoder encoder)
            => writer.Write(Render());

        public override string ToString()
            => Render();

        #endregion
    }
}