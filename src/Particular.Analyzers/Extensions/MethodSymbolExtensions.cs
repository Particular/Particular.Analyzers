namespace Particular.Analyzers.Extensions
{
    using System.Linq;
    using Microsoft.CodeAnalysis;

    public static class MethodSymbolExtensions
    {
        public static bool IsTest(this IMethodSymbol method) =>
            method != null && method.GetAttributes().Any(attribute => IsTestAttribute(attribute.AttributeClass));

        static bool IsTestAttribute(INamedTypeSymbol type) =>
            type != null && (IsTestAttribute(type.ToString()) || IsTestAttribute(type.BaseType));

        static bool IsTestAttribute(string typeName)
        {
            switch (typeName)
            {
                case "NUnit.Framework.OneTimeSetUpAttribute":
                case "NUnit.Framework.OneTimeTearDownAttribute":
                case "NUnit.Framework.SetUpAttribute":
                case "NUnit.Framework.TearDownAttribute":
                case "NUnit.Framework.TestAttribute":
                case "NUnit.Framework.TestCaseAttribute":
                case "NUnit.Framework.TheoryAttribute":
                case "Xunit.FactAttribute":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsEntryPoint(this IMethodSymbol method) =>
            method.ContainingType.Name == "Program" && method.Name == "Main";
    }
}
