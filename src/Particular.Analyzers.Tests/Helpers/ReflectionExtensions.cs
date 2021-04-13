namespace Particular.Analyzers.Tests
{
    using System;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    internal static class ReflectionExtensions
    {
        private static class AssemblyLightUp
        {
            internal static readonly Type Type = typeof(Assembly);

            internal static readonly Func<Assembly, string> GetLocation = Type
                .GetTypeInfo()
                .GetDeclaredMethod("get_Location")
                .CreateDelegate<Func<Assembly, string>>();
        }

        public static string GetLocation(this Assembly assembly)
        {
            if (AssemblyLightUp.GetLocation == null)
            {
                throw new PlatformNotSupportedException();
            }

            return AssemblyLightUp.GetLocation(assembly);
        }

        public static T CreateDelegate<T>(this MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return default(T);
            }

            return (T)(object)methodInfo.CreateDelegate(typeof(T));
        }
    }
}