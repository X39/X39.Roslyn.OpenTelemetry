using Microsoft.CodeAnalysis;
using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public sealed class DiagnosticTests : CompilationTestBaseClass
{
    [Fact]
    public void X39ROTEL0001_NoActivitySourceErrors()
    {
        // Setup
        const string content = """
                               using System.Diagnostics;
                               using X39.Roslyn.OpenTelemetry.Attributes;

                               namespace TestNamespace;
                               public partial class NoActivitySource
                               {
                                   [Activity(ActivityKind.Internal)]
                                   private static partial Activity? StartMyActivity(
                                       string tag
                                   );
                               }
                               """;
        const string activityAttribute = "Activity(ActivityKind.Internal)";
        var activityAttributePosition = content.IndexOf(activityAttribute, StringComparison.Ordinal);

        // Act
        var (runResult, _) = RunCompilation(("NoActivitySource.cs", content));

        // Assert
        Assert.NotEmpty(runResult.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error));
        var errorDiagnostic = Assert.Single(runResult.Diagnostics.Where(d => d.Severity is DiagnosticSeverity.Error));
        Assert.Equal("X39OTEL0001", errorDiagnostic.Id);
        Assert.Equal("Failed to resolve `ActivitySource` for method 'StartMyActivity'", errorDiagnostic.GetMessage());
        Assert.Equal(activityAttributePosition, errorDiagnostic.Location.SourceSpan.Start);
        Assert.Equal(activityAttributePosition + activityAttribute.Length, errorDiagnostic.Location.SourceSpan.End);
    }
}