using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;
using piSensorNet.Logic.Compilation;

namespace piSensorNet.Logic.Triggers
{
    public static class TriggerDelegateCompilerHelper
    {
        private const string Namespace = "piSensorNet.Logic.Compilation.UserFunctions";
        private const string MethodName = "Run";

        private static readonly IReadOnlyCollection<string> Usings 
            = new[]
              {
                  Reflector.Instance<TriggerDelegateContext>.Type.Namespace,
              };

        private static readonly IReadOnlyCollection<string> ReferencedAssemblies 
            = new[]
              {
                  Reflector.Instance<TriggerDelegateContext>.Type.Assembly.Location,
              };

        private static readonly MethodInfo DelegateMethod = TypeExtensions.GetDelegateMethod(Reflector.Instance<TriggerDelegate>.Type);

        private static readonly IReadOnlyDictionary<ParameterInfo, IReadOnlyCollection<PropertyInfo>> DelegateMethodParameters
            = DelegateMethod.GetParameters()
                            .ToDictionary(i => i,
                                i => i.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance).ReadOnly());

        private static string GenerateExplosionCode(IReadOnlyDictionary<string, IReadOnlyDictionary<string, Type>> properties)
        {
            var builder = new StringBuilder();

            foreach (var parameter in DelegateMethodParameters)
                foreach (var parameterProperty in parameter.Value)
                {
                    if (!properties.ContainsKey(parameterProperty.Name))
                    {
                        builder.Append("var ");
                        builder.Append(parameterProperty.Name);
                        builder.Append(" = ");
                        builder.Append(parameter.Key.Name);
                        builder.Append('.');
                        builder.Append(parameterProperty.Name);
                        builder.Append(';');
                        builder.AppendLine();

                        continue;
                    }

                    foreach (var property in properties[parameterProperty.Name])
                    {
                        builder.Append("var ");
                        builder.Append(property.Key);
                        builder.Append(" = ");
                        builder.Append('(');
                        builder.Append(property.Value.GetProperName());
                        builder.Append(')');
                        builder.Append(' ');
                        builder.Append(parameter.Key.Name);
                        builder.Append('.');
                        builder.Append(parameterProperty.Name);
                        builder.Append('[');
                        builder.Append('"');
                        builder.Append(property.Key);
                        builder.Append('"');
                        builder.Append(']');
                        builder.Append(';');
                        builder.AppendLine();
                    }
                }

            return builder.ToString();
        }

        public static CompilationResult<TriggerDelegate> Compile(IReadOnlyDictionary<string, IReadOnlyDictionary<string, Type>> properties, string body)
        {
            var className = $"Generated_{Guid.NewGuid():N}";

            body = GenerateExplosionCode(properties) + Environment.NewLine + body;

            var result = CompileHelper.CompileTo<TriggerDelegate>(Namespace, className, MethodName, body,
                ReferencedAssemblies, Usings);

            return result;
        }
    }
}