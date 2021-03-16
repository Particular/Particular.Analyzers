namespace Particular.CodeRules.Tests.Helpers
{
    using System;
    using System.IO;
    using Microsoft.CodeAnalysis;

    static class CompilationExtensions
    {
        public static void Compile(this Compilation compilation, Action<Diagnostic> onDiagnostic)
        {
            using (var peStream = new MemoryStream())
            {
                var emitResult = compilation.Emit(peStream);

                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    onDiagnostic?.Invoke(diagnostic);
                }

                if (!emitResult.Success)
                {
                    throw new Exception("Compilation failed.");
                }
            }
        }
    }
}
