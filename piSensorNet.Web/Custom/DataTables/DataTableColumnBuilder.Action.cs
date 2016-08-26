using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.AspNet.Mvc.Rendering;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;
using piSensorNet.Web.Custom.DataTables.Interfaces;

namespace piSensorNet.Web.Custom.DataTables
{
    public abstract class DataTableActionColumnBuilder
    {
        internal enum PropertyEnum
        {
            visible,
            title,
            text,
            handler,
            href
        }
    }

    public sealed class DataTableActionColumnBuilder<TElement> : DataTableActionColumnBuilder, IDataTableObjectGenerator
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly IHtmlHelper _htmlHelper;
        private readonly IDictionary<PropertyEnum, object> _properties = new Dictionary<PropertyEnum, object>();

        public string ActionName { get; }

        public DataTableActionColumnBuilder(IHtmlHelper htmlHelper, string name)
        {
            _htmlHelper = htmlHelper;

            ActionName = name;
        }

        public DataTableActionColumnBuilder<TElement> Title(string title)
        {
            _properties.AddOrReplace(PropertyEnum.title, title);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> TitleHtml(JsonLiteral title)
        {
            _properties.AddOrReplace(PropertyEnum.title, title);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Title(Expression<Func<object, int, int, TElement, string>> title)
        {
            _properties.AddOrReplace(PropertyEnum.title, title);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Text(string text)
        {
            _properties.AddOrReplace(PropertyEnum.text, text);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> TextHtml(JsonLiteral text)
        {
            _properties.AddOrReplace(PropertyEnum.text, text);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Icon(string cssClass)
        {
            _properties.AddOrReplace(PropertyEnum.text, (JsonLiteral)$"$('<span />').addClass('fa').addClass('{cssClass}')");

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Visible(bool visible)
        {
            _properties.AddOrReplace(PropertyEnum.visible, visible);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> VisibleHtml(JsonLiteral visible)
        {
            _properties.AddOrReplace(PropertyEnum.visible, visible);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Visible(Expression<Func<object, int, int, TElement, bool>> visible)
        {
            _properties.AddOrReplace(PropertyEnum.visible, visible);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Href(string href)
        {
            _properties.AddOrReplace(PropertyEnum.href, href);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> HrefHtml(JsonLiteral href)
        {
            _properties.AddOrReplace(PropertyEnum.href, href);

            return this;
        }

        public DataTableActionColumnBuilder<TElement> Handler(JsonLiteral handler)
        {
            _properties.AddOrReplace(PropertyEnum.handler, handler);

            return this;
        }

        public object ToObject()
            => _properties;
    }
}