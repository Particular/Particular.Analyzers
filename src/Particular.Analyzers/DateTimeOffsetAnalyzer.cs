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
    public class DateTimeOffsetAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset);

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
            if (!(context.Node is VariableDeclarationSyntax declareVariable))
            {
                return;
            }

            var type = context.SemanticModel.GetTypeInfo(declareVariable.Type, context.CancellationToken).Type;
            if (type.ToString() != "System.DateTimeOffset")
            {
                return;
            }

            var declarator = declareVariable.Variables.First();
            if (declarator == null)
            {
                return;
            }

            var initExpression = declarator.Initializer?.Value;
            if (initExpression == null)
            {
                return;
            }

            var expressionType = context.SemanticModel.GetTypeInfo(initExpression, context.CancellationToken).Type;
            if (expressionType.ToString() != "System.DateTime")
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset, declarator);
        }

        static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is AssignmentExpressionSyntax assignment))
            {
                return;
            }

            var leftType = context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type;
            if (leftType is INamedTypeSymbol leftNamedType && leftNamedType.IsTupleType && leftNamedType.TypeArguments.Any(t => t.ToString() == "System.DateTimeOffset"))
            {
                var rightType = context.SemanticModel.GetTypeInfo(assignment.Right, context.CancellationToken).Type;
                if (rightType is INamedTypeSymbol rightNamedType && rightNamedType.Arity == leftNamedType.Arity)
                {
                    for (var i = 0; i < leftNamedType.TypeArguments.Length; i++)
                    {
                        var tupleLeftType = leftNamedType.TypeArguments[i];
                        if (tupleLeftType.ToString() == "System.DateTimeOffset")
                        {
                            var tupleRightType = rightNamedType.TypeArguments[i];
                            if (tupleRightType.ToString() == "System.DateTime")
                            {
                                context.ReportDiagnostic(DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset, assignment);
                                // Don't want multiple diagnostics on the same location
                                return;
                            }
                        }
                    }
                }
            }
            else if (leftType.ToString() == "System.DateTimeOffset")
            {
                var rightType = context.SemanticModel.GetTypeInfo(assignment.Right, context.CancellationToken).Type;
                if (rightType.ToString() != "System.DateTime")
                {
                    return;
                }

                context.ReportDiagnostic(DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset, assignment);
            }
        }

        static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is MethodDeclarationSyntax method))
            {
                return;
            }

            var returnType = method.ReturnType.ToString();
            if (returnType != "DateTimeOffset" && returnType != "Task<DateTimeOffset>")
            {
                return;
            }

            var returnStatements = method.DescendantNodes().OfType<ReturnStatementSyntax>().ToImmutableArray();

            foreach (var returnStatement in returnStatements)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(returnStatement.Expression, context.CancellationToken);
                if (typeInfo.Type?.ToString() == "System.DateTime")
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset, returnStatement.Expression);
                }
            }
        }

        static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (!(context.Node is InvocationExpressionSyntax invocation))
            {
                return;
            }

            var arguments = invocation.ArgumentList.Arguments;
            IMethodSymbol methodSymbol = null;
            for (var i = 0; i < arguments.Count; i++)
            {
                var argument = arguments[i];
                var typeInfo = context.SemanticModel.GetTypeInfo(argument.Expression, context.CancellationToken);

                if (typeInfo.Type?.ToString() != "System.DateTime")
                {
                    continue;
                }

                if (methodSymbol == null)
                {
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation.Expression, context.CancellationToken);
                    methodSymbol = symbolInfo.Symbol?.GetMethodFromSymbol();
                    if (methodSymbol == null)
                    {
                        continue;
                    }
                }

                if (i >= methodSymbol.Parameters.Length)
                {
                    // We're probably in a Method(params object[] args) like string.Format. Just don't bother.
                    return;
                }

                var parameter = methodSymbol.Parameters[i];
                if (parameter.Type.ToString() == "System.DateTimeOffset")
                {
                    context.ReportDiagnostic(DiagnosticDescriptors.DateTimeAssignedToDateTimeOffset, argument.Expression);
                }
            }
        }
    }
}
