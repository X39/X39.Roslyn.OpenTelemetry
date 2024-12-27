﻿using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public class ActivityRootNodeTests : CompilationTestBaseClass
{
    private const string IsRootIsSetToTrueCode = """
                                                 using System.Diagnostics;
                                                 using X39.Roslyn.OpenTelemetry.Attributes;
                                                 namespace TestNamespace;

                                                 public partial class IsRootIsSetToTrue
                                                 {
                                                     [Activity(ActivityKind.Internal, IsRoot = true, CreateActivitySource = true)]
                                                     private static partial Activity? StartMyActivity();
                                                 }
                                                 """;

    private const string IsRootIsSetToTrueExpected = """
                                                     // <auto-generated/>
                                                     #nullable enable
                                                     using System.Diagnostics;
                                                     using System.ComponentModel;
                                                     using System.Collections.Generic;

                                                     namespace TestNamespace;
                                                     partial class IsRootIsSetToTrue
                                                     {
                                                         private static ActivitySource MyActivitySource = new("My");
                                                         private static partial Activity? StartMyActivity()
                                                         {
                                                             return MyActivitySource.StartActivity(
                                                                 "My",
                                                                 ActivityKind.Internal,
                                                                 parentContext: new ActivityContext(Activity.TraceIdGenerator is null ? ActivityTraceId.CreateRandom() : Activity.TraceIdGenerator(), default, default, default)
                                                             );
                                                         }
                                                     }

                                                     """;


    [Fact]
    public void IsRootingWorkingIfExplicitlySetToTrue()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(IsRootIsSetToTrueCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("IsRootIsSetToTrue", ".", "My", ".g.cs"))
        );
        Assert.Equal(IsRootIsSetToTrueExpected, classOutput, ignoreLineEndingDifferences: true);
    }

    private const string IsRootIsSetToFalseCode = """
                                                  using System.Diagnostics;
                                                  using X39.Roslyn.OpenTelemetry.Attributes;
                                                  namespace TestNamespace;

                                                  public partial class IsRootIsSetToFalse
                                                  {
                                                      [Activity(ActivityKind.Internal, IsRoot = false, CreateActivitySource = true)]
                                                      private static partial Activity? StartMyActivity();
                                                  }
                                                  """;

    private const string IsRootIsSetToFalseExpected = """
                                                      // <auto-generated/>
                                                      #nullable enable
                                                      using System.Diagnostics;
                                                      using System.ComponentModel;
                                                      using System.Collections.Generic;

                                                      namespace TestNamespace;
                                                      partial class IsRootIsSetToFalse
                                                      {
                                                          private static ActivitySource MyActivitySource = new("My");
                                                          private static partial Activity? StartMyActivity()
                                                          {
                                                              return MyActivitySource.StartActivity(
                                                                  "My",
                                                                  ActivityKind.Internal
                                                              );
                                                          }
                                                      }

                                                      """;


    [Fact]
    public void IsRootingWorkingIfExplicitlySetToFalse()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(IsRootIsSetToFalseCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("IsRootIsSetToFalse", ".", "My", ".g.cs"))
        );
        Assert.Equal(IsRootIsSetToFalseExpected, classOutput, ignoreLineEndingDifferences: true);
    }

    private const string IsRootIsSetToTrueAndActivityContextIsPassedCode = """
                                                                           using System.Diagnostics;
                                                                           using X39.Roslyn.OpenTelemetry.Attributes;
                                                                           namespace TestNamespace;

                                                                           public partial class IsRootIsSetToTrueAndActivityContextIsPassed
                                                                           {
                                                                               [Activity(ActivityKind.Internal, IsRoot = true, CreateActivitySource = true)]
                                                                               private static partial Activity? StartMyActivity(ActivityContext context);
                                                                           }
                                                                           """;

    private const string IsRootIsSetToTrueAndActivityContextIsPassedExpected = """
                                                                               // <auto-generated/>
                                                                               #nullable enable
                                                                               using System.Diagnostics;
                                                                               using System.ComponentModel;
                                                                               using System.Collections.Generic;

                                                                               namespace TestNamespace;
                                                                               partial class IsRootIsSetToTrueAndActivityContextIsPassed
                                                                               {
                                                                                   private static ActivitySource MyActivitySource = new("My");
                                                                                   private static partial Activity? StartMyActivity(
                                                                                       System.Diagnostics.ActivityContext context
                                                                                   )
                                                                                   {
                                                                                       return MyActivitySource.StartActivity(
                                                                                           "My",
                                                                                           ActivityKind.Internal,
                                                                                           parentContext: context
                                                                                       );
                                                                                   }
                                                                               }

                                                                               """;


    [Fact]
    public void IsRootingNotUsedIfExplicitlySetToTrueAndActivityContextIsParameter()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(IsRootIsSetToTrueAndActivityContextIsPassedCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("IsRootIsSetToTrueAndActivityContextIsPassed", ".", "My", ".g.cs"))
        );
        Assert.Equal(IsRootIsSetToTrueAndActivityContextIsPassedExpected, classOutput, ignoreLineEndingDifferences: true);
    }
}