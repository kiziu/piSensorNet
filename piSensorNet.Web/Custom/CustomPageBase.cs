using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Internal;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.AspNet.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using piSensorNet.Web.Custom.DataTables;
using piSensorNet.Web.Extensions;

namespace piSensorNet.Web.Custom
{
    public abstract class BaseCustomPageBase : RazorPage
    {
        public const string ExpressionViewResourcesItemsKey = "ExpressionViewResources";
        public const string RawViewResourcesItemsKey = "RawViewResources";

        public static Nested JSON { get { throw new NotImplementedException(); } }
        public static Nested piSensorNet { get { throw new NotImplementedException(); } }

        static BaseCustomPageBase()
        {
            DataTable.Fields.Add($"{nameof(BaseCustomPageBase)}.{nameof(JSON)}");
            DataTable.Fields.Add($"{nameof(BaseCustomPageBase)}.{nameof(piSensorNet)}");
        }

        internal BaseCustomPageBase() {}

        protected HtmlString RenderResources([NotNull] string resourcesPath, int indentationLevel = 0, bool skipFirstIndentation = true)
        {
            var expressionResourcesList = Context.GetItem<List<Expression<Func<string>>>>(ExpressionViewResourcesItemsKey, false);
            var rawResourcesList = Context.GetItem<Dictionary<string, string>>(RawViewResourcesItemsKey, false);
            if (expressionResourcesList?.Count == 0 && rawResourcesList?.Count == 0)
                return null;

            var pathItems = resourcesPath.Split(new[] {"."}, StringSplitOptions.RemoveEmptyEntries);
            if (pathItems.Length < 1)
                throw new ArgumentException("Resource path is too short, at least one level must present.", nameof(resourcesPath));

            var path = "window";
            var jsBuilder = new StringBuilder();

            var indentation = new string('\t', indentationLevel);

            if (!skipFirstIndentation)
                jsBuilder.Append(indentation);

            jsBuilder.Append("{");
            jsBuilder.AppendLine();
            foreach (var pathItem in pathItems)
            {
                jsBuilder.Append(indentation);
                jsBuilder.Append("\t");

                jsBuilder.Append($"!{path}.hasOwnProperty('{pathItem}') && jQuery.extend({path}, {{ '{pathItem}': {{}} }});");

                jsBuilder.AppendLine();

                path += "." + pathItem;
            }

            jsBuilder.Append(indentation);
            jsBuilder.Append("\r\n");

            if (expressionResourcesList != null)
                foreach (var resource in expressionResourcesList)
                {
                    var memberExpression = resource.Body as MemberExpression;
                    if (memberExpression == null)
                        throw new ArgumentException($"{resource} is not a correct resource expression.", "resource");

                    var key = memberExpression.Member.DeclaringType.Name + "_" + memberExpression.Member.Name;
                    var value = resource.Compile().Invoke();

                    jsBuilder.Append(indentation);
                    jsBuilder.Append("\t");

                    jsBuilder.Append($"{path}.{key} = '{value}';");

                    jsBuilder.AppendLine();
                }

            if (rawResourcesList != null)
                foreach (var resource in rawResourcesList)
                {
                    jsBuilder.Append(indentation);
                    jsBuilder.Append("\t");

                    jsBuilder.Append($"{path}.{resource.Key} = '{resource.Value}';");

                    jsBuilder.AppendLine();
                }

            jsBuilder.Append(indentation);
            jsBuilder.Append("}");

            var js = jsBuilder.ToString();

            return new HtmlString(js);
        }
    }

    public abstract class CustomPageBase<TModel> : BaseCustomPageBase
    {
        private IModelMetadataProvider _provider;

        /// <summary>
        /// Gets the Model property of the <see cref="P:Microsoft.AspNet.Mvc.Razor.RazorPage`1.ViewData" /> property.
        /// </summary>
        public TModel Model => ViewData != null ? ViewData.Model : default(TModel);

        /// <summary>Gets or sets the dictionary for view data.</summary>
        [RazorInject]
        public ViewDataDictionary<TModel> ViewData { get; set; }

        /// <summary>
        /// Returns a <see cref="T:Microsoft.AspNet.Mvc.Rendering.ModelExpression" /> instance describing the given <paramref name="expression" />.
        /// </summary>
        /// <typeparam name="TValue">The type of the <paramref name="expression" /> result.</typeparam>
        /// <param name="expression">An expression to be evaluated against the current model.</param>
        /// <returns>A new <see cref="T:Microsoft.AspNet.Mvc.Rendering.ModelExpression" /> instance describing the given <paramref name="expression" />.
        /// </returns>
        /// <remarks>
        /// Compiler normally infers <typeparamref name="TValue" /> from the given <paramref name="expression" />.
        /// </remarks>
        public ModelExpression CreateModelExpression<TValue>(Expression<Func<TModel, TValue>> expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            if (_provider == null)
                _provider = Context.RequestServices.GetRequiredService<IModelMetadataProvider>();

            var expressionText = ExpressionHelper.GetExpressionText(expression);
            var modelExplorer = ExpressionMetadataProvider.FromLambdaExpression(expression, ViewData, _provider);

            if (modelExplorer == null)
                throw new InvalidOperationException($"The {nameof(IModelMetadataProvider)} was unable to provide metadata for expression '{expressionText}'.");

            return new ModelExpression(expressionText, modelExplorer);
        }
    }

    public abstract class CustomPageBase : CustomPageBase<object> {}
}