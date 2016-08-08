using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;

namespace piSensorNet.Logic.Compilation
{
    public static class CompileHelper
    {
        private const string ClassPattern = @"using System;
using System.Linq;
using System.Collections.Generic;

namespace {0}
{{
    internal static class {1}
    {{
        public static {2}
        {{
            {3}
        }}
    }}
}}";
        internal static readonly int ErrorLineOffset = -10;
        private const string Namespace = "piSensorNet.Logic.Compilation.UserFunctions";
        private const string MethodName = "Run";

        private static readonly ConcurrentDictionary<Type, string> Signatures = new ConcurrentDictionary<Type, string>();
        private static readonly string IndentString = new String(' ', 12);
        private static readonly string[] LineSeparators = {"\r\n", "\n"};

        internal static readonly int ErrorColumnOffset = -IndentString.Length;

        private static readonly CompilerParameters CompilerParameters = new CompilerParameters
                                                                        {
                                                                            CompilerOptions = "/target:library /optimize",
                                                                            GenerateExecutable = false,
                                                                            GenerateInMemory = true,
                                                                            IncludeDebugInformation = true,
                                                                            TreatWarningsAsErrors = false,
                                                                            ReferencedAssemblies =
                                                                            {
                                                                                "System.dll",
                                                                                "System.Core.dll"
                                                                            },
                                                                        };

        private static readonly CSharpCodeProvider CodeProvider = new CSharpCodeProvider(new Dictionary<string, string>
                                                                                         {
                                                                                             {"CompileVersion", "v4.0"}
                                                                                         });

        private static string PrepareMethodBody(string body)
        {
            var builder = new StringBuilder();
            var lines = body.Split(LineSeparators, StringSplitOptions.None);

            if (lines.Length == 0)
                return String.Empty;

            builder.Append(lines[0]);

            for (var i = 1; i < lines.Length; i++)
            {
                builder.Append('\n');
                builder.Append(IndentString);
                builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        public static CompilationResult<TDelegate> CompileTo<TDelegate>(string body)
            where TDelegate : class
        {
            var delegateType = Reflector.Instance<TDelegate>.Type;
            if (!delegateType.IsSubclassOf(Reflector.Instance<Delegate>.Type))
                throw new ArgumentException($"Type '{delegateType.Name}' is not a delegate type.", nameof(TDelegate));

            var signature = Signatures.GetOrAdd(delegateType, i => TypeExtensions.GetSignature(i, MethodName));

            var methodCode = PrepareMethodBody(body);
            var className = $"Generated_{Guid.NewGuid():N}";
            var classCode = ClassPattern.AsFormatFor(Namespace, className, signature, methodCode);
            
            var result = CodeProvider.CompileAssemblyFromSource(CompilerParameters, classCode);
            if (result.Errors.HasErrors)
                return new CompilationResult<TDelegate>(classCode, result.Errors);

            var methodInfo = result.CompiledAssembly.GetType($"{Namespace}.{className}").GetMethod(MethodName);
            var function = (TDelegate)(object)Delegate.CreateDelegate(delegateType, methodInfo);

            return new CompilationResult<TDelegate>(classCode, function);
        }
    }
}
