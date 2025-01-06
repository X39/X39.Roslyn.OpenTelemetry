using System;
using Microsoft.CodeAnalysis;

namespace X39.Roslyn.OpenTelemetry.Generator.ExtensionMethods;

public static class AccessibilityExt
{

    public static string ToCSharpString(this Accessibility accessibility)
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
}