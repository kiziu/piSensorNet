using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.Web.Custom;
using piSensorNet.Web.Custom.DataTables;

namespace piSensorNet.Web.Extensions
{
    public static class HttpContextExtensions
    {
        [ContractAnnotation("initNonexistent:true => notnull")]
        [CanBeNull]
        public static T GetItem<T>(this HttpContext context, object key, bool initNonexistent = true)
            where T : new()
        {
            T value;
            if (!context.Items.ContainsKey(key))
            {
                if (!initNonexistent)
                    return default(T);

                value = new T();

                context.Items[key] = value;
            }
            else
                value = (T)context.Items[key];

            return value;
        }
    }

    public static class HtmlHelperExtensions
    {
        private const string ReadonlyAttribute = "readonly";
        private const string DisabledAttribute = "disabled";

        public static void Resources([NotNull] this IHtmlHelper htmlHelper, [NotNull] params Expression<Func<string>>[] resources)
        {
            var expressionResources = htmlHelper.ViewContext
                                                .HttpContext
                                                .GetItem<List<Expression<Func<string>>>>(
                                                    CustomPageBase.ExpressionViewResourcesItemsKey);

            expressionResources.AddRange(resources);
        }

        public static void Resources<TEnum>([NotNull] this IHtmlHelper htmlHelper)
            where TEnum : struct
        {
            var localized = LocalizationExtensions.LocalizeAllKeyed<TEnum>();
            var rawResources = htmlHelper.ViewContext
                                         .HttpContext
                                         .GetItem<Dictionary<string, string>>(
                                             CustomPageBase.RawViewResourcesItemsKey);

            rawResources.Add(localized);
        }

        [NotNull]
        public static string ResourcePrefix<TResources, TEnum>(this IHtmlHelper htmlHelper)
            where TResources : class
            where TEnum : struct
            => Reflector.Instance<TResources>.Name + "_" + Reflector.Instance<TEnum>.EnumName;

        [NotNull]
        public static IHtmlContent ReadonlyTextBox([NotNull] this IHtmlHelper htmlHelper, [NotNull] string name, object value,
            [CanBeNull] string format, [CanBeNull] object htmlAttributes, bool isDisabled = false)
            => htmlHelper.TextBox(name, value,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)
                          .AddOrReplace(ReadonlyAttribute, ReadonlyAttribute)
                          .If(isDisabled, i => i.AddOrReplace(DisabledAttribute, DisabledAttribute)));

        [NotNull]
        public static IHtmlContent ReadonlyTextBox([NotNull] this IHtmlHelper htmlHelper, [NotNull] string name, object value,
            [CanBeNull] object htmlAttributes, bool isDisabled = false)
            => ReadonlyTextBox(htmlHelper, name, value, null, htmlAttributes, isDisabled);

        [NotNull]
        public static IHtmlContent ReadonlyTextBoxFor<TModel, TResult>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression, [NotNull] string format, [CanBeNull] object htmlAttributes = null,
            bool isDisabled = false)
            => htmlHelper.TextBoxFor(expression, format,
                HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes)
                          .AddOrReplace(ReadonlyAttribute, ReadonlyAttribute)
                          .If(isDisabled, i => i.AddOrReplace(DisabledAttribute, DisabledAttribute)));

        [NotNull]
        public static IHtmlContent ReadonlyTextBoxFor<TModel, TResult>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression, [CanBeNull] object htmlAttributes = null, bool isDisabled = false)
            // ReSharper disable once AssignNullToNotNullAttribute
            => ReadonlyTextBoxFor(htmlHelper, expression, null, htmlAttributes, isDisabled);

        [NotNull]
        public static IHtmlContent ValidationMessageFor<TModel, TResult>([NotNull] this IHtmlHelper<TModel> htmlHelper,
            [NotNull] Expression<Func<TModel, TResult>> expression, [CanBeNull] object htmlAttributes = null)
            => htmlHelper.ValidationMessageFor(expression, null, htmlAttributes);

        [NotNull]
        public static DataTable<TElement> DataTable<TElement>([NotNull] this IHtmlHelper htmlHelper, [NotNull] string tableID)
            => new DataTable<TElement>(htmlHelper, tableID);
    }
}