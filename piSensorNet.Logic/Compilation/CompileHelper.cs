using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;
using Microsoft.CSharp;
using piSensorNet.Common.Extensions;
using piSensorNet.Common.System;

namespace piSensorNet.Logic.Compilation
{
    public static class CompileHelper
    {
        private static readonly IReadOnlyCollection<string> DefaultUsings
            = new[]
              {
                  "System",
                  "System.Linq",
                  "System.Collections.Generic"
              };

        private static readonly IReadOnlyCollection<string> DefaultReferencedAssemblies
            = new[]
              {
                  "System.dll",
                  "System.Core.dll"
              };

        private const string ClassPattern = @"{0}

namespace {1}
{{
    internal static class {2}
    {{
        public static {3}
        {{{4}
        }}
    }}
}}";

        internal static readonly int ErrorLineOffset = -10;

        private static readonly ConcurrentDictionary<Type, string> Signatures
            = new ConcurrentDictionary<Type, string>();

        private static readonly string IndentString = new String('\t', 4);
        private static readonly string[] LineSeparators = {"\r\n", "\n"};

        internal static readonly int ErrorColumnOffset = -IndentString.Length;

        private static readonly CSharpCodeProvider CodeProvider
            = new CSharpCodeProvider(new Dictionary<string, string>
                                     {
                                         {"CompileVersion", "v4.0"}
                                     });

        private static string PrepareMethodBody(string body)
        {
            var lines = body.Split(LineSeparators, StringSplitOptions.None);

            if (lines.Length == 0)
                return String.Empty;

            var builder = new StringBuilder();

            foreach (var line in lines)
            {
                builder.Append('\n');
                builder.Append(IndentString);
                builder.Append(line);
            }

            return builder.ToString();
        }

        [NotNull]
        public static CompilationResult<TDelegate> CompileTo<TDelegate>([NotNull] string @namespace,
            [NotNull] string className, [NotNull] string methodName, [NotNull] string body,
            IReadOnlyCollection<string> referencedAssemblies = null,
            IReadOnlyCollection<string> usings = null)
            where TDelegate : class
        {
            var delegateType = Reflector.Instance<TDelegate>.Type;
            if (!delegateType.IsSubclassOf(Reflector.Instance<Delegate>.Type))
                throw new ArgumentException($"Type '{delegateType.Name}' is not a delegate type.", nameof(TDelegate));

            var signature = Signatures.GetOrAdd(delegateType, i => TypeExtensions.GetDelegateSignature(i, "{0}"))
                                      .AsFormatFor(methodName);

            var methodCode = PrepareMethodBody(body);

            var usingSection = (usings ?? DefaultUsings).Select(i => $"using {i};").Join(Environment.NewLine);
            var classCode = ClassPattern.AsFormatFor(usingSection, @namespace, className, signature, methodCode);

            var compilerParameters = new CompilerParameters
                                     {
                                         CompilerOptions = "/target:library /optimize",
                                         GenerateExecutable = false,
                                         GenerateInMemory = true,
                                         IncludeDebugInformation = true,
                                         TreatWarningsAsErrors = false,
                                     };

            (referencedAssemblies ?? DefaultReferencedAssemblies).Each(compilerParameters.ReferencedAssemblies.Add);

            var result = CodeProvider.CompileAssemblyFromSource(compilerParameters, classCode);
            if (result.Errors.HasErrors)
                return new CompilationResult<TDelegate>(classCode, result.Errors);

            var methodInfo = result.CompiledAssembly.GetType($"{@namespace}.{className}").GetMethod(methodName);

            var function = (TDelegate)(object)Delegate.CreateDelegate(delegateType, methodInfo);

            return new CompilationResult<TDelegate>(classCode, function);
        }
    }
}