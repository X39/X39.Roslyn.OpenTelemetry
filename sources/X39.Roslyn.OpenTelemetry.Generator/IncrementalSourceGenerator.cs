using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using X39.Roslyn.OpenTelemetry.Generator.ExtensionMethods;
using X39.Roslyn.OpenTelemetry.Generator.Statics;


namespace X39.Roslyn.OpenTelemetry.Generator;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class IncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is MethodDeclarationSyntax,
                (ctx, _) => GetMethodDeclarationForSourceGen(ctx)
            )
            .Where(t => t.reportAttributeFound)
            .Select((t, _) => t.Item1);

        // Generate the source code.
        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right))
        );
    }

    /// <summary>
    /// Checks whether the Node is annotated with the [Report] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
    /// </summary>
    /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
    /// <returns>The specific cast and whether the attribute was found.</returns>
    private static (MethodDeclarationSyntax, bool reportAttributeFound) GetMethodDeclarationForSourceGen(
        GeneratorSyntaxContext context
    )
    {
        var methodDeclarationSyntax = (MethodDeclarationSyntax) context.Node;

        // Go through all attributes of the class.
        foreach (var attributeSyntax in methodDeclarationSyntax.AttributeLists.SelectMany(
                     attributeListSyntax => attributeListSyntax.Attributes
                 ))
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax)
                    .Symbol is not IMethodSymbol attributeSymbol)
                continue; // if we can't get the symbol, ignore it

            var attributeName = attributeSymbol.ContainingType.ToDisplayString();

            switch (attributeName)
            {
                // Check the full name of the [Report] attribute.
                case Constants.ActivityAttribute:
                case Constants.ClientActivityAttribute:
                case Constants.ConsumerActivityAttribute:
                case Constants.InternalActivityAttribute:
                case Constants.ProducerActivityAttribute:
                case Constants.ServerActivityAttribute:
                    return (methodDeclarationSyntax, true);
            }
        }

        return (methodDeclarationSyntax, false);
    }

    private void GenerateCode(
        SourceProductionContext context,
        Compilation compilation,
        ImmutableArray<MethodDeclarationSyntax> methodDeclarations
    )
    {
        // Go through all filtered class declarations.
        foreach (var methodDeclarationSyntax in methodDeclarations)
        {
            // We need to get semantic model of the class to retrieve metadata.
            var semanticModel = compilation.GetSemanticModel(methodDeclarationSyntax.SyntaxTree);

            // Symbols allow us to get the compile-time information.
            if (semanticModel.GetDeclaredSymbol(methodDeclarationSyntax) is not IMethodSymbol methodSymbol)
                continue;
            if (methodDeclarationSyntax.Parent is not ClassDeclarationSyntax classDeclarationSyntax)
                continue;
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;
            if (GenerationInfo.Resolve(classSymbol, methodSymbol) is not { } generationInfo)
                continue;

            var namespaceName = methodSymbol.ContainingNamespace.ToDisplayString();

            // 'Identifier' means the token of the node. Get class name from the syntax node.
            var fullClassName = string.Concat(
                classDeclarationSyntax.Identifier,
                classDeclarationSyntax.TypeParameterList
            );
            var fileClassName = fullClassName.Replace('<', '_')
                .Replace('>', '_')
                .Replace(',', '_')
                .Replace(" ", string.Empty);

            var (activitySourceReference, activityKind, activityName, isRoot, createActivitySource) = generationInfo;

            if (!ValidateActivitySourceReferenceNotEmpty(context, methodSymbol, activitySourceReference))
                continue;

            var parameters = methodSymbol.Parameters
                .Select(parameter => (type: parameter.Type.ToDisplayString(), name: parameter.Name))
                .ToList();

            var builder = new StringBuilder();
            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine($"#nullable enable");
            builder.AppendLine($"using System.Diagnostics;");
            builder.AppendLine($"using System.ComponentModel;");
            builder.AppendLine($"using System.Collections.Generic;");
            builder.AppendLine();
            builder.AppendLine($"namespace {namespaceName};");
            builder.AppendLine($"partial class {fullClassName}");
            builder.AppendLine("{");


            if (createActivitySource)
            {
                builder.AppendLine(
                    $"    private static ActivitySource {Constants.CodeGen.ActivitySourcePrefix}{activityName}{Constants.CodeGen.ActivitySourceSuffix} = new(\"{activityName.ToCSharpString()}\");"
                );
            }

            var hasParameters = parameters.Count > 0;
            var hasActivityContextOrTags = hasParameters || isRoot;
            var activityContextName = default(string);
            if (parameters.Count is 0)
            {
                builder.AppendLine(
                    $"    {methodSymbol.DeclaredAccessibility.ToCSharpString()} {(methodSymbol.IsStatic ? "static " : "")}partial Activity? {methodSymbol.Name}()"
                );
            }
            else
            {
                builder.AppendLine(
                    $"    {methodSymbol.DeclaredAccessibility.ToCSharpString()} {(methodSymbol.IsStatic ? "static " : "")}partial Activity? {methodSymbol.Name}("
                );
                foreach (var (index, type, name) in parameters.Select((t, i) => (index: i, t.type, t.name)))
                {
                    if (type == "System.Diagnostics.ActivityContext")
                        activityContextName = name;

                    builder.AppendLine($"        {type} {name}{(index < parameters.Count - 1 ? "," : "")}");
                }

                builder.AppendLine($"    )");
            }

            builder.AppendLine(@"    {");
            builder.AppendLine($"        return {activitySourceReference}.StartActivity(");
            builder.AppendLine($"            \"{activityName.ToCSharpString()}\",");
            builder.AppendLine($"            ActivityKind.{activityKind}{(hasActivityContextOrTags ? "," : "")}");
            if (hasActivityContextOrTags)
            {
                var hasTags = activityContextName is not null && parameters.Count > 1
                              || activityContextName is null && parameters.Count > 0;

                builder.AppendLine(
                    $"            parentContext: {GetActivityContextValue(activityContextName, isRoot)}{(hasTags ? "," : "")}"
                );
                if (hasTags)
                {
                    builder.AppendLine($"            tags: new[] {{");
                    foreach (var (type, name) in parameters)
                    {
                        if (type == "System.Diagnostics.ActivityContext")
                            continue;
                        builder.AppendLine(
                            $"                new KeyValuePair<string, object?>(\"{name.ToCSharpString()}\", {name}),"
                        );
                    }

                    builder.AppendLine($"            }}");
                }
            }

            builder.AppendLine($"        );");
            builder.AppendLine(@"    }");
            builder.AppendLine(@"}");

            // Add the source code to the compilation.
            context.AddSource(
                $"{fileClassName}.{activityName}.g.cs",
                SourceText.From(builder.ToString(), Encoding.UTF8)
            );
        }
    }

    private bool ValidateActivitySourceReferenceNotEmpty(
        SourceProductionContext context,
        IMethodSymbol methodSymbol,
        string activitySourceReference
    )
    {
        if (activitySourceReference.Length > 0)
            return true;

        context.ReportDiagnostic(
            Diagnostic.Create(
                Diagnostics.ActivitySourceCouldNotBeResolved,
                methodSymbol.GetAttributes()
                    .GetActivityAttributes()
                    .First()
                    .ApplicationSyntaxReference
                    ?.GetSyntax()
                    .GetLocation(),
                methodSymbol.Name
            )
        );

        return false;
    }

    private string GetActivityContextValue(string? activityContextName, bool isRoot)
    {
        if (activityContextName is not null)
            return activityContextName;
        if (isRoot)
            return
                "new ActivityContext(Activity.TraceIdGenerator is null ? ActivityTraceId.CreateRandom() : Activity.TraceIdGenerator(), default, default, default)";
        return "default";
    }

}