namespace Particular.Analyzers.Tests.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    public class TestCustomizations
    {
        readonly HashSet<Type> typesForMetadataReferences = [
            typeof(object),
            typeof(Enumerable)
        ];

        public CompilationOptions CompilationOptions { get; set; }

        public TestCustomizations AddMetadataReferenceUsing<T>()
        {
            typesForMetadataReferences.Add(typeof(T));
            return this;
        }

        public IEnumerable<PortableExecutableReference> GetMetadataReferences()
        {
            var arr = typesForMetadataReferences
                .Select(type => type.GetTypeInfo().Assembly)
                .Distinct()
                .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                .ToArray();

            return arr;
        }
    }
}
