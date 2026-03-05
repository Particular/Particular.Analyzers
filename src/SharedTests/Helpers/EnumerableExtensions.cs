namespace Particular.Analyzers.Tests.Helpers
{
    using System.Collections.Generic;
    using System.Linq;
    using Data = System.Collections.Generic.IEnumerable<object[]>;

    static class EnumerableExtensions
    {
        public static Data ToData<T>(this IEnumerable<T> source) =>
            source.Select(item => new object[] { item });

        // tuple sources must be split up to ensure the data can be serialized
        // so that each test case shows up separately in the test runner
        public static Data ToData<T1, T2>(this IEnumerable<(T1, T2)> source) =>
            source.Select(item => new object[] { item.Item1, item.Item2 });

        public static Data ToData<T1, T2, T3>(this IEnumerable<(T1, T2, T3)> source) =>
            source.Select(item => new object[] { item.Item1, item.Item2, item.Item3 });
    }
}
