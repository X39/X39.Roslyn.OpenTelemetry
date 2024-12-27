﻿using X39.Util;
using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public class EncapsulationTests : CompilationTestBaseClass
{
    private const string ArgActivityCode = """
                                           using System.Diagnostics;
                                           using X39.Roslyn.OpenTelemetry.Attributes;
                                           namespace TestNamespace;

                                           public partial class {1}ActivityTest
                                           {{
                                               [Activity(ActivityKind.Internal, CreateActivitySource = true)]
                                               {0} static partial Activity? StartMyActivity();
                                           }}
                                           """;

    private const string ArgActivityExpected = """
                                               // <auto-generated/>
                                               #nullable enable
                                               using System.Diagnostics;
                                               using System.ComponentModel;
                                               using System.Collections.Generic;

                                               namespace TestNamespace;
                                               partial class {1}ActivityTest
                                               {{
                                                   private static ActivitySource MyActivitySource = new("My");
                                                   {0} static partial Activity? StartMyActivity()
                                                   {{
                                                       return MyActivitySource.StartActivity(
                                                           "My",
                                                           ActivityKind.Internal
                                                       );
                                                   }}
                                               }}

                                               """;


    // @formatter:max_line_length 5000
    [Theory]
    [InlineData("public")]
    [InlineData("private")]
    [InlineData("protected")]
    [InlineData("internal")]
    [InlineData("protected internal", "protectedInternal")]
    // @formatter:max_line_length restore
    public void AllEncapsulationsWork(string encapsulation, string? classPrefix = null)
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(ArgActivityCode.Format(encapsulation, classPrefix ?? encapsulation), []);

        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat(classPrefix ?? encapsulation, "ActivityTest", ".", "My", ".g.cs"))
        );
        Assert.Equal(
            ArgActivityExpected.Format(encapsulation, classPrefix ?? encapsulation),
            classOutput,
            ignoreLineEndingDifferences: true
        );
    }
}