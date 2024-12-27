using Microsoft.CodeAnalysis;

namespace X39.Roslyn.OpenTelemetry.Generator;

public static class Diagnostics
{
    public static readonly DiagnosticDescriptor ActivitySourceCouldNotBeResolved = new DiagnosticDescriptor(
        id: Constants.CodeGen.DiagnosticPrefix + "0001",
        title: "Failed to resolve `ActivitySource` for method",
        messageFormat: "Failed to resolve `ActivitySource` for method '{0}'",
        description: """
                     The source generator could not resolve the `ActivitySource` for the method.
                     The reason for this could be that:
                     - The method is static and the `ActivitySource` present in the class is not static.
                       In this case, adjust the modifier of either the method or the `ActivitySource` to be similar.
                     - There is no `ActivitySource` present in the class and neither the method nor the class are
                       decorated with the `ActivitySourceReferenceAttribute`.
                       In this case, add the `ActivitySourceReferenceAttribute` to the class or method.
                     If you intended for the method to be auto-generating an `ActivitySource`, make sure
                     to set "CreateActivitySource" to "true" in the attribute.
                     """,
        category: "X39.Roslyn.OpenTelemetry.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
    
    
}