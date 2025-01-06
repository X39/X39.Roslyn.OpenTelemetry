using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using X39.Roslyn.OpenTelemetry.Generator.Statics;

namespace X39.Roslyn.OpenTelemetry.Generator.ExtensionMethods;

public static class AttributeDataEnumerableExt
{
    public static IEnumerable<AttributeData> GetGeneratorAttributes(this IEnumerable<AttributeData> attributes)
        => attributes.Where(attribute => attribute.AttributeClass!.ContainingNamespace.ToDisplayString()== Constants.Namespace
    );

    public static IEnumerable<AttributeData> GetActivityAttributes(this IEnumerable<AttributeData> attributes)
    {
        foreach (var attributeData in GetGeneratorAttributes(attributes))
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
                    yield return attributeData;
                    break;
            }
        }
    }
}