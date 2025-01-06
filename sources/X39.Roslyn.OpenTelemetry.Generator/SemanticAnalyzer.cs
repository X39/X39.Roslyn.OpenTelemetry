using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using X39.Roslyn.OpenTelemetry.Generator.Statics;

namespace X39.Roslyn.OpenTelemetry.Generator;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class SemanticAnalyzer : DiagnosticAnalyzer
{
    // We do not have to do anything but do this for IDE support
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create([
        Diagnostics.ActivitySourceCouldNotBeResolved,
    ]);

    public override void Initialize(AnalysisContext context)
    {
        // Default stuff, not sure if really needed, given we don't register anything else, but meh ...
        // better be safe than sorry
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
    }
}