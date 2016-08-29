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

        private static readonly MethodInfo DelegateMethod = TypeExtensions.GetDelegateMethod(Reflector.Instance<TriggerDelegate>.Type);

        private static string GenerateExplosionCode(IReadOnlyDictionary<string, IReadOnlyDictionary<string, Type>> properties)
        {
            var builder = new StringBuilder();

            foreach (var parameter in DelegateMethod.GetParameters())
                foreach (var parameterProperty in parameter.ParameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (!properties.ContainsKey(parameterProperty.Name))
                    {
                        builder.Append("var ");
                        builder.Append(parameterProperty.Name);
                        builder.Append(" = ");
                        builder.Append(parameter.Name);
                        builder.Append('.');
                        builder.Append(parameterProperty.Name);
                        builder.Append(';');
                        builder.AppendLine();

                        continue;
                    }

                    foreach (var property in properties[parameterProperty.Name])
                    {
                        builder.Append(property.Value.GetProperName());
                        builder.Append(' ');
                        builder.Append(property.Key);
                        builder.Append(" = ");
                        builder.Append(parameter.Name);
                        builder.Append('.');
                        builder.Append(parameterProperty.Name);
                        builder.Append('.');
                        builder.Append(property.Key);
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

            var result = CompileHelper.CompileTo<TriggerDelegate>(Namespace, className, MethodName, body);

            return result;
        }
    }
}
