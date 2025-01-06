using Microsoft.CodeAnalysis;
using X39.Roslyn.OpenTelemetry.Generator.Properties;

namespace X39.Roslyn.OpenTelemetry.Generator.Statics;

public static class Diagnostics
{
    private static LocalizableString Localize(string name)
        => new LocalizableResourceString(name, Localizations.ResourceManager, typeof(Localizations));

    public static readonly DiagnosticDescriptor ActivitySourceCouldNotBeResolved = new(
        id: Constants.CodeGen.DiagnosticPrefix + "0001",
        title: Localize(nameof(Localizations.Diagnostics_ActivitySourceCouldNotBeResolved_Title)),
        messageFormat: Localize(nameof(Localizations.Diagnostics_ActivitySourceCouldNotBeResolved_MessageFormat)),
        description: Localize(nameof(Localizations.Diagnostics_ActivitySourceCouldNotBeResolved_Description)),
        category: "X39.Roslyn.OpenTelemetry.Generator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true
    );
}