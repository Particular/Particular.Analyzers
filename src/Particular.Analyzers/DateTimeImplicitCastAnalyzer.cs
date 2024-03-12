namespace Particular.Analyzers.Cancellation
{
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Particular.Analyzers.Extensions;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DateTimeImplicitCastAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclaration, SyntaxKind.VariableDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not VariableDeclarationSyntax declaration)
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(declaration.Type, context.CancellationToken).Type;
            if (type?.ToString() != "System.DateTimeOffset")
            {
                return;
            }

            foreach (var declarator in declaration.Variables)
            {
                var initializer = declarator.Initializer?.Value;
                if (initializer == null)
                {
                    continue;
                }

                var initializerType = context.SemanticModel.GetTypeInfo(initializer, context.CancellationToken).Type;
                if (initializerType?.ToString() != "System.DateTime")
                {
                    continue;
                }

                context.ReportDiagnostic(DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset, declarator);
            }
        }

        static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not AssignmentExpressionSyntax assignment)
            {
                return;
            }

            if (context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type is not INamedTypeSymbol leftType)
            {
                return;
            }

            if (leftType.IsTupleType && leftType.TypeArguments.Any(t => t.ToString() == "System.DateTimeOffset"))
            {
                if (context.SemanticModel.GetTypeInfo(assignment.Right, context.CancellationToken).Type is not INamedTypeSymbol rightType)
                {
                    return;
                }

                if (rightType.Arity != leftType.Arity)
                {
                    return;
                }

                for (var i = 0; i < leftType.TypeArguments.Length; i++)
                {
                    if (leftType.TypeArguments[i].ToString() != "System.DateTimeOffset")
                    {
                        continue;
                    }

                    if (rightType.TypeArguments[i].ToString() != "System.DateTime")
                    {
                        continue;
                    }

                    context.ReportDiagnostic(DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset, assignment);

                    // Don't want multiple diagnostics on the same location
                    return;
                }
            }
            else if (leftType.ToString() == "System.DateTimeOffset")
            {
                var rightType = context.SemanticModel.GetTypeInfo(assignment.Right, context.CancellationToken).Type;

                if (rightType?.ToString() != "System.DateTime")
                {
                    return;
                }

                context.ReportDiagnostic(DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset, assignment);
            }
        }

        static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not MethodDeclarationSyntax method)
            {
                return;
            }

            var returnType = method.ReturnType.ToString();

            if (returnType is not "DateTimeOffset" and not "Task<DateTimeOffset>")
            {
                return;
            }

            var returnStatementExpressions = method.DescendantNodes()
                .OfType<ReturnStatementSyntax>()
                .Select(returnStatement => returnStatement.Expression)
                .OfType<ExpressionSyntax>()
                .ToImmutableArray();

            foreach (var returnStatement in returnStatementExpressions)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(returnStatement, context.CancellationToken);

                if (typeInfo.Type?.ToString() == "System.DateTime")
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset, returnStatement);
                }
            }
        }

        static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            if (!invocation.ArgumentList.Arguments.Any())
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken).Symbol?.GetMethodOrDefault() is not IMethodSymbol method)
            {
                return;
            }

            var args = invocation.ArgumentList.Arguments;

            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];
                var typeInfo = context.SemanticModel.GetTypeInfo(arg.Expression, context.CancellationToken);

                if (typeInfo.Type?.ToString() != "System.DateTime")
                {
                    continue;
                }

                if (i >= method.Parameters.Length)
                {
                    // We're probably in a Method(params object[] args) like string.Format. Just don't bother.
                    return;
                }

                var param = method.Parameters[i];

                if (param.Type.ToString() == "System.DateTimeOffset")
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.ImplicitCastFromDateTimeToDateTimeOffset, arg);
                }
            }
        }
    }
}
