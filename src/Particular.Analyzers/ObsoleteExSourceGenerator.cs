namespace Particular.Analyzers;

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[Generator]
public class ObsoleteExSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find types and properties marked with ObsoleteEx attribute
        var membersWithObsoleteEx = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "ObsoleteExAttribute",
                (node, _) => node is TypeDeclarationSyntax or PropertyDeclarationSyntax,
                (context, _) => context.TargetNode)
            .Collect();

        // Combine with compilation to access version information
        var compilationAndMembers = context.CompilationProvider.Combine(membersWithObsoleteEx);

        // Generate source based on assembly version and obsolete metadata
        context.RegisterSourceOutput(compilationAndMembers, (context, source) =>
        {
            var (compilation, members) = source;
            foreach (var member in members)
            {
                var semanticModel = compilation.GetSemanticModel(member.SyntaxTree);
                var memberSymbol = semanticModel.GetDeclaredSymbol(member);

                if (memberSymbol == null)
                {
                    continue;
                }

                var obsoleteExAttribute = memberSymbol.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.Name == "ObsoleteExAttribute");

                if (obsoleteExAttribute == null)
                {
                    continue;
                }

                // Extract attribute parameters
                var message = obsoleteExAttribute.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "Message")
                    .Value.Value?.ToString() ?? "";
                var treatAsErrorFromVersion = obsoleteExAttribute.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "TreatAsErrorFromVersion")
                    .Value.Value?.ToString() ?? "";
                var removeInVersion = obsoleteExAttribute.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "RemoveInVersion")
                    .Value.Value?.ToString() ?? "";
                var replacementType = obsoleteExAttribute.NamedArguments
                    .FirstOrDefault(arg => arg.Key == "ReplacementTypeOrMember")
                    .Value.Value?.ToString() ?? "";

                // Get current assembly version
                var assemblyVersion = compilation.Assembly.Identity.Version;

                // Generate appropriate obsolete attribute based on version
                string generatedObsoleteAttribute = GenerateObsoleteAttribute(
                    message,
                    replacementType,
                    treatAsErrorFromVersion,
                    removeInVersion,
                    assemblyVersion);

                // If a build error should be generated
                if (generatedObsoleteAttribute == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor(
                            "OBSL001",
                            "Obsolete Member Removal",
                            $"Cannot process '{memberSymbol.Name}'. The assembly version {assemblyVersion} is higher than version specified in 'RemoveInVersion' {removeInVersion}. The member should be removed or 'RemoveInVersion' increased.",
                            "Lifecycle",
                            DiagnosticSeverity.Error,
                            true),
                        member.GetLocation()));
                    continue;
                }

                // Generate source with new attribute
                string memberType = member switch
                {
                    TypeDeclarationSyntax typeSyntax => $"partial {typeSyntax.Keyword}",
                    PropertyDeclarationSyntax => "partial class",
                    _ => "partial"
                };

                context.AddSource(
                    $"{memberSymbol.Name}_Obsolete.cs",
                    $$"""
                      using System;
                      using System.ComponentModel;

                      {{generatedObsoleteAttribute}}
                      {{memberType}} {{(member is TypeDeclarationSyntax ? memberSymbol.Name : memberSymbol.ContainingType.Name)}} 
                      {
                          {{(member is PropertyDeclarationSyntax ? $"public {(memberSymbol as IPropertySymbol)?.Type.Name} {memberSymbol.Name} {{ get; set; }}" : "")}}
                      }
                      """);
            }
        });
        ;
    }

    static string GenerateObsoleteAttribute(
        string message,
        string replacementType,
        string treatAsErrorFromVersion,
        string removeInVersion,
        Version assemblyVersion)
    {
        var removeVersion = Version.Parse(removeInVersion);
        var treatAsErrorVersion = Version.Parse(treatAsErrorFromVersion);

        // Build attribute message
        string obsoleteMessage = $"{message}. Use '{replacementType}' instead.";

        // Determine appropriate obsolete attribute based on current version
        if (assemblyVersion < removeVersion)
        {
            // Treat as warning
            obsoleteMessage +=
                $" Will be treated as an error from version {treatAsErrorFromVersion}. Will be removed in version {removeInVersion}.";
            return $"[Obsolete(\"{obsoleteMessage}\")]";
        }
        else if (assemblyVersion >= removeVersion && assemblyVersion < treatAsErrorVersion)
        {
            // Treat as error
            obsoleteMessage += $" Will be removed in version {removeInVersion}.";
            return $"[Obsolete(\"{obsoleteMessage}\", true)]\n[EditorBrowsable(EditorBrowsableState.Advanced)]";
        }
        else
        {
            // Generate build error
            return null;
        }
    }
}