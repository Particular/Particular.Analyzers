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
            context.RegisterSyntaxNodeAction(Analyze,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.VariableDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is AssignmentExpressionSyntax assignment)
            {
                AnalyzeAssignment(context, assignment);
            }
            else if (context.Node is VariableDeclarationSyntax declareVariable)
            {
                AnalyzeVariableDeclaration(context, declareVariable);
            }
        }

        static void AnalyzeVariableDeclaration(SyntaxNodeAnalysisContext context, VariableDeclarationSyntax declareVariable)
        {
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

        static void AnalyzeAssignment(SyntaxNodeAnalysisContext context, AssignmentExpressionSyntax assignment)
        {
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
    }
}
