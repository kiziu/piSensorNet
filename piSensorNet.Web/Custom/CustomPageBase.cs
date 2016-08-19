using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Rendering;
using piSensorNet.Web.Extensions;

namespace piSensorNet.Web.Custom
{
    public abstract class CustomPageBase<TModel> : RazorPage<TModel>
    {
        public const string ExpressionViewResourcesItemsKey = "ExpressionViewResources";
        public const string RawViewResourcesItemsKey = "RawViewResources";

        protected HtmlString RenderResources([NotNull] string resourcesPath, int indentationLevel = 0, bool skipFirstIndentation = true)
        {
            var expressionResourcesList = Context.GetItem<List<Expression<Func<string>>>>(ExpressionViewResourcesItemsKey, false);
            var rawResourcesList = Context.GetItem<Dictionary<string, string>>(RawViewResourcesItemsKey, false);
            if (expressionResourcesList?.Count == 0 && rawResourcesList?.Count == 0)
                return null;

            var pathItems = resourcesPath.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
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

    public abstract class CustomPageBase : CustomPageBase<object>
    {

    }
}
