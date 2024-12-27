using System.Linq;
using Microsoft.CodeAnalysis;

namespace X39.Roslyn.OpenTelemetry.Generator;

internal record struct GenerationInfo(
    string ActivitySourceReference,
    string ActivityKind,
    string ActivityName,
    bool IsRoot,
    bool CreateActivitySource
)
{
    public static GenerationInfo? Resolve(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol)
    {
        var activitySourceReference = ResolveActivitySourceReference(classSymbol);

        GenerationInfo? generationInfo = null;
        foreach (var attributeData in methodSymbol.GetAttributes()
                     .Where(
                         attribute => attribute.AttributeClass!.ContainingNamespace.ToDisplayString()
                                      == Constants.Namespace
                     ))
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is null)
                continue;
            var attributeName = attributeClass.ToDisplayString();

            switch (attributeName)
            {
                case Constants.ActivityAttribute:
                case Constants.ClientActivityAttribute:
                case Constants.ConsumerActivityAttribute:
                case Constants.InternalActivityAttribute:
                case Constants.ProducerActivityAttribute:
                case Constants.ServerActivityAttribute:
                    generationInfo = ResolveActivityAttribute(
                        methodSymbol,
                        attributeName,
                        attributeData,
                        generationInfo
                    );
                    break;
                case Constants.ActivitySourceReferenceAttribute:
                    activitySourceReference = attributeData.ConstructorArguments.FirstOrDefault()
                                                  .Value as string
                                              ?? string.Empty;
                    break;
            }
        }

        if (generationInfo is null)
            return null;
        activitySourceReference = string.IsNullOrWhiteSpace(generationInfo.Value.ActivitySourceReference)
            ? activitySourceReference
            : generationInfo.Value.ActivitySourceReference;
        if (string.IsNullOrWhiteSpace(activitySourceReference))
        {
            activitySourceReference = ResolveActivitySourceOnMember(classSymbol, methodSymbol);
        }
        return generationInfo.Value with
        {
            ActivitySourceReference = string.IsNullOrWhiteSpace(generationInfo.Value.ActivitySourceReference)
                ? activitySourceReference
                : generationInfo.Value.ActivitySourceReference,
        };
    }

    private static string ResolveActivitySourceOnMember(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol)
    {
        // Attempt to find an accessible activitySource field or property on the symbol.
        var methodIsStatic = methodSymbol.IsStatic;

        var fields = classSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => field.CanBeReferencedByName);
        foreach (var fieldSymbol in fields)
        {
            if ((!methodIsStatic || fieldSymbol.IsStatic) && fieldSymbol.Type.ToDisplayString() == "System.Diagnostics.ActivitySource")
                return fieldSymbol.Name;
        }
        var properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>();
        foreach (var propertySymbol in properties)
        {
            if ((!methodIsStatic || propertySymbol.IsStatic) && propertySymbol.Type.ToDisplayString() == "System.Diagnostics.ActivitySource")
                return propertySymbol.Name;
        }

        return "";
    }

    private static string ResolveActivitySourceReference(INamedTypeSymbol classSymbol)
    {
        var activitySourceReferenceAttribute = classSymbol.GetAttributes()
            .FirstOrDefault(
                attribute => attribute.AttributeClass!.ContainingNamespace.ToDisplayString() == Constants.Namespace
                             && attribute.AttributeClass.ToDisplayString() == Constants.ActivitySourceReferenceAttribute
            );
        return activitySourceReferenceAttribute?.ConstructorArguments.FirstOrDefault()
                   .Value as string
               ?? string.Empty;
    }


    private static GenerationInfo? ResolveActivityAttribute(
        IMethodSymbol methodSymbol,
        string attributeName,
        AttributeData attributeData,
        GenerationInfo? generationInfo
    )
    {
        #region ActivityKind

        string activityKind;

        switch (attributeName)
        {
            case Constants.ActivityAttribute:
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
            case Constants.ClientActivityAttribute:
                activityKind = "Client";
                break;
            case Constants.ConsumerActivityAttribute:
                activityKind = "Consumer";
                break;
            case Constants.InternalActivityAttribute:
                activityKind = "Internal";
                break;
            case Constants.ProducerActivityAttribute:
                activityKind = "Producer";
                break;
            case Constants.ServerActivityAttribute:
                activityKind = "Server";
                break;
            default:
                return generationInfo;
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

        #region IsRoot

        var isRoot = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "IsRoot")
                         .Value.Value as bool?
                     ?? false;

        #endregion

        #region CreateActivitySource

        var createActivitySource = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "CreateActivitySource")
                                       .Value.Value as bool?
                                   ?? false;

        #endregion

        return new GenerationInfo(
            ActivitySourceReference: createActivitySource
                ? string.Concat(Constants.CodeGen.ActivitySourcePrefix, activityName, Constants.CodeGen.ActivitySourceSuffix)
                : generationInfo?.ActivitySourceReference ?? string.Empty,
            ActivityKind: activityKind,
            ActivityName: activityName,
            IsRoot: isRoot,
            CreateActivitySource: createActivitySource
        );
    }
}