using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using piSensorNet.Common.Custom;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.JsonConverters;
using piSensorNet.Common.System;

namespace piSensorNet.Web.Custom.DataTables
{
    internal sealed class ExpressionFuncConverter : JsonConverter
    {
        private static readonly Type OpenTypeOfExpression = typeof(Expression<>);
        private readonly IReadOnlyDictionary<string, string> _fields;

        private static readonly Func<JsonWriter, string> GetIndentation;

        static ExpressionFuncConverter()
        {
            var writer = Expression.Parameter(Reflector.Instance<JsonWriter>.Type, "writer");
            var textWriter = Expression.Convert(writer, Reflector.Instance<JsonTextWriter>.Type);
            var indentChar = Expression.PropertyOrField(textWriter, nameof(JsonTextWriter.IndentChar));
            var indentation = Expression.PropertyOrField(textWriter, nameof(JsonTextWriter.Indentation));
            var top = Expression.PropertyOrField(textWriter, "Top");
            var totalIndentation = Expression.Multiply(indentation, top);
            // ReSharper disable once AssignNullToNotNullAttribute
            var indentString = Expression.New(Reflector.Instance<String>.Constructor<char, int>(), indentChar, totalIndentation);

            GetIndentation = Expression.Lambda<Func<JsonWriter, string>>(indentString, writer).Compile();
        }

        public ExpressionFuncConverter(params string[] fields)
        {
            _fields = fields.Select(i => new {Whole = i, Split = i.Split('.')}).ToDictionary(i => i.Whole, i => i.Split.Last());
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var expression = (LambdaExpression)value;

            var parameters = expression.Parameters;
            if (parameters.Count > 0)
            {
                writer.WriteWhitespace(" ");
                writer.WriteRaw("(");
                writer.WriteRaw(parameters.Select(i => i.Name).Join(", "));
                writer.WriteRaw(")");

                writer.WriteRaw(" =>");
            }

            var body = expression.Body;
            string bodyValue;
            if (body.Type == Reflector.Instance<Boolean>.Type)
            {
                var literal = new StringBuilder(body.ToString());

                literal.Replace($".{nameof(VariousDataTablesExtensions.Value)}()", $".{ExtendedEnumConverter.ValueKey}");

                literal.Replace(nameof(ExpressionType.OrElse), "||");
                literal.Replace(nameof(ExpressionType.AndAlso), "&&");
                literal.RegexReplace(@"([^\.]+)\.([a-z0-9]_)", "$1['$2']", RegexOptions.IgnoreCase);
                literal.RegexReplace($"{nameof(ExpressionType.Convert)}\\(([^\\)]+)\\)", "$1", RegexOptions.IgnoreCase);
                literal.RegexReplace($"{nameof(ExpressionType.ArrayLength)}\\(([a-z0-9_\\.]+)\\.{nameof(VariousDataTablesExtensions.AsArray)}\\(\\)\\)", $"$1.{nameof(Array.Length).ToLowerInvariant()}", RegexOptions.IgnoreCase);

                bodyValue = literal.ToString();
            }
            else if (body.Type == Reflector.Instance<Nested>.Type)
            {
                var pairs = new Dictionary<string, string>();
                var visited = new Visitor(pairs).Visit(body);
                var literal = new StringBuilder(visited.ToString());

                literal.Replace(_fields);

                literal.RegexReplace(@"\.get_Item\(""([^""]+)""\)", ".$1", RegexOptions.IgnoreCase);
                literal.RegexReplace($"\\.{nameof(Nested.Invoke)}\\(new \\[\\] {{([^}}]+)}}\\)", "($1)", RegexOptions.IgnoreCase);

                var lineSeperator = Environment.NewLine + GetIndentation(writer);

                literal.Replace(pairs, lines => lines.Split(DataTable.JsonLineSeparator, StringSplitOptions.None)
                                                     .Join(lineSeperator));

                bodyValue = literal.ToString();
            }
            else if (body.Type == Reflector.Instance<JsonLiteral>.Type
                     || body.Type == Reflector.Instance<string>.Type)
            {
                var visited = new Visitor().Visit(body);
                var literal = new StringBuilder(visited.ToString());
                
                literal.Replace($".{nameof(Nested.Invoke)}(new [] {{}})", "()");
                literal.Replace($".{nameof(VariousDataTablesExtensions.Value)}()", $".{ExtendedEnumConverter.ValueKey}");

                literal.Replace(_fields);

                literal.RegexReplace(@".get_Item\(""([^""]+)""\)", ".$1", RegexOptions.IgnoreCase);
                literal.RegexReplace($"\\.{nameof(Nested.Invoke)}\\(new \\[\\] {{([^}}]+)}}\\)", "($1)", RegexOptions.IgnoreCase);
                literal.RegexReplace(@"([^\.]+)\.([a-z0-9]_)", "$1['$2']", RegexOptions.IgnoreCase);
                literal.RegexReplace($"{nameof(ExpressionType.Convert)}\\(([^\\)]+)\\)", "$1", RegexOptions.IgnoreCase);

                bodyValue = literal.ToString();
            }
            else
                throw new NotSupportedException();

            writer.WriteRawValue(bodyValue);
        }

        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType)
                return false;

            var genericType = objectType.GetGenericTypeDefinition();
            if (genericType != OpenTypeOfExpression)
                return false;

            var genericArgument = objectType.GetGenericArguments()[0];

            return genericArgument.FullName.StartsWith("System.Func`", StringComparison.InvariantCulture);
        }

        internal class Visitor : ExpressionVisitor
        {
            private readonly IDictionary<string, string> _pairs;

            public Visitor([CanBeNull] IDictionary<string, string> pairs = null)
            {
                _pairs = pairs;
            }
            
            protected override Expression VisitMethodCall(MethodCallExpression node)
            {
                if (node.Method.Name == "get_Item" || node.Method.DeclaringType == Reflector.Instance<Nested>.Type)
                    return base.VisitMethodCall(node);

                var value = Expression.Lambda<Func<object>>(node).Compile()();
                if (_pairs == null)
                    return Expression.Constant(value);

                var key = Guid.NewGuid().ToString("N");

                _pairs.Add($"\"{key}\"", Common.Helpers.JsonSerializer.Serialize(value, DataTable.JsonConverters.Value));

                return Expression.Constant(key);
            }

            protected override Expression VisitMember(MemberExpression node)
            {
                if (node.Expression != null || node.Type == Reflector.Instance<Nested>.Type)
                    return base.VisitMember(node);

                return Expression.Constant(Expression.Lambda<Func<object>>(node).Compile()());
            }
        }
    }
}