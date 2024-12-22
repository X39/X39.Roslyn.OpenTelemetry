using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace X39.Roslyn.OpenTelemetry.Generator;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class ActivitySourceGenerator : IIncrementalGenerator
{
    private const string Namespace                 = "X39.Roslyn.OpenTelemetry.Attributes";
    private const string ActivityAttribute         = Namespace + "." + "ActivityAttribute";
    private const string ClientActivityAttribute   = Namespace + "." + "ClientActivityAttribute";
    private const string ConsumerActivityAttribute = Namespace + "." + "ConsumerActivityAttribute";
    private const string InternalActivityAttribute = Namespace + "." + "InternalActivityAttribute";
    private const string ProducerActivityAttribute = Namespace + "." + "ProducerActivityAttribute";
    private const string ServerActivityAttribute   = Namespace + "." + "ServerActivityAttribute";


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
                case ActivityAttribute:
                case ClientActivityAttribute:
                case ConsumerActivityAttribute:
                case InternalActivityAttribute:
                case ProducerActivityAttribute:
                case ServerActivityAttribute:
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
            if (Resolve(
                    methodSymbol,
                    methodSymbol.GetAttributes()
                        .Where(
                            attribute => attribute.AttributeClass!.ContainingNamespace.ToDisplayString() == Namespace
                        )
                ) is not { } generationInfo)
                continue;
            if (methodDeclarationSyntax.Parent is not ClassDeclarationSyntax classDeclarationSyntax)
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

            var (activityKind, activityName, activitySourceIdentifier, isRoot) = generationInfo;

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
            builder.AppendLine($"    [EditorBrowsable(EditorBrowsableState.Never)]");
            builder.AppendLine(
                $"    private static ActivitySource {activitySourceIdentifier} = new(\"{StringEscape(activityName)}\");"
            );
            var hasParameters = parameters.Count > 0;
            var hasActivityContextOrTags = hasParameters || isRoot;
            var activityContextName = default(string);
            if (parameters.Count is 0)
            {
                builder.AppendLine(
                    $"    {ToEncapsulation(methodSymbol.DeclaredAccessibility)} static partial Activity? {methodSymbol.Name}()"
                );
            }
            else
            {
                builder.AppendLine(
                    $"    {ToEncapsulation(methodSymbol.DeclaredAccessibility)} static partial Activity? {methodSymbol.Name}("
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
            builder.AppendLine($"        return {activitySourceIdentifier}.StartActivity(");
            builder.AppendLine($"            \"{StringEscape(activityName)}\",");
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
                            $"                new KeyValuePair<string, object?>(\"{StringEscape(name)}\", {name}),"
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

    private string GetActivityContextValue(string? activityContextName, bool isRoot)
    {
        if (activityContextName is not null)
            return activityContextName;
        if (isRoot)
            return
                "new ActivityContext(Activity.TraceIdGenerator is null ? ActivityTraceId.CreateRandom() : Activity.TraceIdGenerator(), default, default, default)";
        return "default";
    }

    private string ToEncapsulation(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => "",
            Accessibility.Private => "private",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "protected internal",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.Public => "public",
            _ => throw new ArgumentOutOfRangeException(nameof(accessibility), accessibility, null),
        };
    }

    private string StringEscape(string activityName) => activityName.Replace("\"", "\\\"");

    private GenerationInfo? Resolve(IMethodSymbol methodSymbol, IEnumerable<AttributeData> attributeDatas)
    {
        foreach (var attributeData in attributeDatas)
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null)
                continue;
            var attributeName = attributeClass.ToDisplayString();

            #region ActivityKind

            string activityKind;

            switch (attributeName)
            {
                case ActivityAttribute:
                    activityKind = attributeData.ConstructorArguments.FirstOrDefault()
                            .Value switch
                        {
                            0 => "Internal",
                            1 => "Server",
                            2 => "Client",
                            3 => "Producer",
                            4 => "Consumer",
                            _ => string.Empty,
                        };
                    break;
                case ClientActivityAttribute:
                    activityKind = "Client";
                    break;
                case ConsumerActivityAttribute:
                    activityKind = "Consumer";
                    break;
                case InternalActivityAttribute:
                    activityKind = "Internal";
                    break;
                case ProducerActivityAttribute:
                    activityKind = "Producer";
                    break;
                case ServerActivityAttribute:
                    activityKind = "Server";
                    break;
                default:
                    continue;
            }

            #endregion

            #region ActivityName

            var activityName = methodSymbol.Name;
            if (activityName.StartsWith("Start"))
            {
                activityName = activityName.Substring("Start".Length);
            }

            if (activityName.EndsWith("Activity"))
            {
                activityName = activityName.Substring(0, activityName.Length - "Activity".Length);
            }

            var activityNameArg = attributeData.NamedArguments
                .FirstOrDefault(kvp => kvp.Key == "ActivityName")
                .Value
                .Value
                ?.ToString();

            if (!string.IsNullOrEmpty(activityNameArg))
            {
                activityName = activityNameArg!;
            }

            #endregion

            #region ActivitySourceIdentifier

            var activitySourceIdentifierArg = attributeData.NamedArguments
                                                  .FirstOrDefault(kvp => kvp.Key == "Identifier")
                                                  .Value
                                                  .Value
                                                  ?.ToString()
                                              ?? string.Empty;

            var activitySourceIdentifier = !string.IsNullOrEmpty(activitySourceIdentifierArg)
                ? activitySourceIdentifierArg
                : $"{activityName}ActivitySource";

            #endregion

            #region IsRoot

            var isRoot = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "IsRoot")
                             .Value.Value as bool?
                         ?? false;

            #endregion

            return new GenerationInfo(activityKind, activityName, activitySourceIdentifier, isRoot);
        }

        return null;
    }
}

internal record struct GenerationInfo(
    string ActivityKind,
    string ActivityName,
    string ActivitySourceIdentifier,
    bool IsRoot
);