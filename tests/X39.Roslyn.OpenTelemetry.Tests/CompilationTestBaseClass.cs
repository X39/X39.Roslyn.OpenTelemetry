using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using X39.Roslyn.OpenTelemetry.Generator;
using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public class CompilationTestBaseClass
{
    protected static (string FilePath, string Code)[] AssertCompilationAndGetGeneratedFiles(
        string code,
        params string[] acceptedErrors
    )
    {
        var (runResult, newCompilation) = RunCompilation(("Test.cs", code));

        // Verify that the compilation has no errors.
        var diagnostics = newCompilation.GetDiagnostics();
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && !acceptedErrors.Contains(d.Id)));

        // All generated files can be found in 'RunResults.GeneratedTrees'.
        var generatedFiles = runResult.GeneratedTrees
            .Where(t => t.FilePath.EndsWith(".g.cs"))
            .Select(
                (q) => (q.FilePath, Code: q.GetText()
                    .ToString())
            )
            .ToArray();
        return generatedFiles;
    }

    protected static (GeneratorDriverRunResult runResult, Compilation newCompilation) RunCompilation(
        params IEnumerable<(string filePath, string content)> files
    )
    {
        // Create an instance of the source generator.
        var generator = new IncrementalSourceGenerator();

        // Source generators should be tested using 'GeneratorDriver'.
        var driver = CSharpGeneratorDriver.Create(generator);

        // We need to create a compilation with the required source code.
        var assemblies = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .OrderBy((q) => q.Location)
            .ToArray();
        var assemblyDir = Path.GetDirectoryName(
                              assemblies.Single(q => q.Location.EndsWith("netstandard.dll"))
                                  .Location
                          )
                          ?? throw new InvalidOperationException(
                              "Could not find the directory of the netstandard.dll assembly."
                          );
        var selfDir = Path.GetDirectoryName(
            assemblies.Single(q => q.Location.EndsWith("X39.Roslyn.OpenTelemetry.Tests.dll"))
                .Location
        );
        Assert.NotNull(selfDir);
        var compilation = CSharpCompilation.Create(
            nameof(SpecializedAttributesTests),
            files.Select(t => CSharpSyntaxTree.ParseText(t.content, path: t.filePath)),
            assemblies.Select(assembly => assembly.Location)
                .Append(Path.Combine(selfDir, "X39.Roslyn.OpenTelemetry.dll"))
                .Append(Path.Combine(assemblyDir, "System.ComponentModel.Annotations.dll"))
                .Append(Path.Combine(assemblyDir, "System.Diagnostics.DiagnosticSource.dll"))
                .Select(path => MetadataReference.CreateFromFile(path))
                .Distinct()
                .ToArray(),
            new CSharpCompilationOptions(
                outputKind: OutputKind.DynamicallyLinkedLibrary,
                reportSuppressedDiagnostics: true,
                optimizationLevel: OptimizationLevel.Debug
            )
        );
        // Run generators and retrieve all results.
        var runResult = driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var _)
            .GetRunResult();
        return (runResult, newCompilation);
    }
}