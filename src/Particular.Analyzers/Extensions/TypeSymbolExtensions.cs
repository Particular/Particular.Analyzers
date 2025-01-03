﻿namespace Particular.Analyzers.Extensions
{
    using System;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    public static class TypeSymbolExtensions
    {
        public static bool IsCancellationToken(this ITypeSymbol? type) =>
            type?.ToString() == "System.Threading.CancellationToken";

        public static bool IsAsyncDisposable(this ITypeSymbol? type) =>
            type?.ToString() == "System.IAsyncDisposable";

        public static bool IsEventArgs(this ITypeSymbol? typeSymbol)
        {
            if (typeSymbol is ITypeParameterSymbol typeParameter)
            {
                return typeParameter.ConstraintTypes.Any(constraintType => constraintType.IsEventArgs());
            }

            while (typeSymbol != null)
            {
                if (typeSymbol.ToString() == "System.EventArgs")
                {
                    return true;
                }
                typeSymbol = typeSymbol.BaseType;
            }
            return false;
        }

        public static bool IsCancellableContext(this ITypeSymbol? type)
        {
            if (type == null)
            {
                return false;
            }

            if (type is ITypeParameterSymbol typeParameter)
            {
                return typeParameter.ConstraintTypes.Any(constraintType => constraintType.IsCancellableContext());
            }

            if (IsCancellableContext(type.ToString()))
            {
                return true;
            }

            return type.AllInterfaces.Any(@interface => IsCancellableContext(@interface.ToString()));
        }

        static bool IsCancellableContext(string type) =>
            type == "NServiceBus.ICancellableContext";

        public static bool IsTask(this ITypeSymbol? type) =>
            type != null && (IsTask(type.ToString()) || type.BaseType.IsTask());

        static bool IsTask(string? type) =>
            type != null &&
            (type == "System.Threading.Tasks.Task" ||
                type == "System.Threading.Tasks.ValueTask" ||
                type.StartsWith("System.Threading.Tasks.ValueTask<", StringComparison.Ordinal));

        public static bool IsConfiguredTaskAwaitable(this ITypeSymbol? type) =>
            type != null && IsConfiguredTaskAwaitable(type.ToString());

        static bool IsConfiguredTaskAwaitable(string type) =>
            type == "System.Runtime.CompilerServices.ConfiguredTaskAwaitable" ||
            type.StartsWith("System.Runtime.CompilerServices.ConfiguredTaskAwaitable<", StringComparison.Ordinal);

        public static bool IsFunc(this INamedTypeSymbol? type) =>
            type is { TypeArguments.Length: > 0 } && type.ToString().StartsWith("System.Func<", StringComparison.Ordinal);
    }
}
