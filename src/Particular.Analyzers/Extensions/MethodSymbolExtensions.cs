﻿namespace Particular.Analyzers.Extensions
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
            return typeName switch
            {
                "NUnit.Framework.OneTimeSetUpAttribute" or "NUnit.Framework.OneTimeTearDownAttribute" or "NUnit.Framework.SetUpAttribute" or "NUnit.Framework.TearDownAttribute" or "NUnit.Framework.TestAttribute" or "NUnit.Framework.TestCaseAttribute" or "NUnit.Framework.TestCaseSourceAttribute" or "NUnit.Framework.TheoryAttribute" or "Xunit.FactAttribute" => true,
                _ => false,
            };
        }

        public static bool IsEntryPoint(this IMethodSymbol method) =>
            method.ContainingType.Name == "Program" && method.Name == "Main";
    }
}
