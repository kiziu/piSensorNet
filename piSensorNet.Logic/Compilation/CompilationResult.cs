using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace piSensorNet.Logic.Compilation
{
    public sealed class CompilationResult<T>
    {
        [NotNull]
        public string Body { get; }

        [CanBeNull]
        public T Method { get; }

        [CanBeNull]
        public IReadOnlyCollection<CompilerError> CompilerErrors { get; }
        
        public bool IsSuccessful => CompilerErrors == null || CompilerErrors.Count == 0;

        internal CompilationResult(string body, T method)
        {
            Body = body;
            Method = method;
        }

        public CompilationResult(string body, CompilerErrorCollection compilerErrors)
        {
            Body = body;
            CompilerErrors = compilerErrors.Cast<CompilerError>()
                                           .OrderBy(i => i.Line)
                                           .ThenBy(i => i.Column)
                                           .Select(i => new CompilerError(i.FileName, i.Line + CompileHelper.ErrorLineOffset,
                                               i.Column + CompileHelper.ErrorColumnOffset, i.ErrorNumber, i.ErrorText))
                                           .ToList();
        }
    }
}