﻿using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public class ActivityContextPassingTests : CompilationTestBaseClass
{
    private const string ActivityContextPassedAloneCode = """
                                                          using System.Diagnostics;
                                                          using X39.Roslyn.OpenTelemetry.Attributes;
                                                          namespace TestNamespace;

                                                          public partial class ActivityContextPassedAlone
                                                          {
                                                              [Activity(ActivityKind.Internal, CreateActivitySource = true)]
                                                              private static partial Activity? StartMyActivity(System.Diagnostics.ActivityContext context);
                                                          }
                                                          """;

    private const string ActivityContextPassedAloneExpected = """
                                                              // <auto-generated/>
                                                              #nullable enable
                                                              using System.Diagnostics;
                                                              using System.ComponentModel;
                                                              using System.Collections.Generic;

                                                              namespace TestNamespace;
                                                              partial class ActivityContextPassedAlone
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
    public void PassingActivityContextAloneWillFillParentContext()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(ActivityContextPassedAloneCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("ActivityContextPassedAlone", ".", "My", ".g.cs"))
        );
        Assert.Equal(ActivityContextPassedAloneExpected, classOutput, ignoreLineEndingDifferences: true);
    }


    private const string ActivityContextPassedWithStringTagRightCode = """
                                                                       using System.Diagnostics;
                                                                       using X39.Roslyn.OpenTelemetry.Attributes;
                                                                       namespace TestNamespace;

                                                                       public partial class ActivityContextPassedWithStringTagRight
                                                                       {
                                                                           [Activity(ActivityKind.Internal, CreateActivitySource = true)]
                                                                           private static partial Activity? StartMyActivity(ActivityContext context, string tag);
                                                                       }
                                                                       """;

    private const string ActivityContextPassedWithStringTagRightExpected = """
                                                                           // <auto-generated/>
                                                                           #nullable enable
                                                                           using System.Diagnostics;
                                                                           using System.ComponentModel;
                                                                           using System.Collections.Generic;

                                                                           namespace TestNamespace;
                                                                           partial class ActivityContextPassedWithStringTagRight
                                                                           {
                                                                               private static ActivitySource MyActivitySource = new("My");
                                                                               private static partial Activity? StartMyActivity(
                                                                                   System.Diagnostics.ActivityContext context,
                                                                                   string tag
                                                                               )
                                                                               {
                                                                                   return MyActivitySource.StartActivity(
                                                                                       "My",
                                                                                       ActivityKind.Internal,
                                                                                       parentContext: context,
                                                                                       tags: new[] {
                                                                                           new KeyValuePair<string, object?>("tag", tag),
                                                                                       }
                                                                                   );
                                                                               }
                                                                           }

                                                                           """;


    [Fact]
    public void PassingActivityContextWithTagOnRightSideWillFillParentContext()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(ActivityContextPassedWithStringTagRightCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("ActivityContextPassedWithStringTagRight", ".", "My", ".g.cs"))
        );
        Assert.Equal(ActivityContextPassedWithStringTagRightExpected, classOutput, ignoreLineEndingDifferences: true);
    }


    private const string ActivityContextPassedWithStringTagLeftCode = """
                                                                      using System.Diagnostics;
                                                                      using X39.Roslyn.OpenTelemetry.Attributes;
                                                                      namespace TestNamespace;

                                                                      public partial class ActivityContextPassedWithStringTagLeft
                                                                      {
                                                                          [Activity(ActivityKind.Internal, CreateActivitySource = true)]
                                                                          private static partial Activity? StartMyActivity(string tag, ActivityContext context);
                                                                      }
                                                                      """;

    private const string ActivityContextPassedWithStringTagLeftExpected = """
                                                                          // <auto-generated/>
                                                                          #nullable enable
                                                                          using System.Diagnostics;
                                                                          using System.ComponentModel;
                                                                          using System.Collections.Generic;

                                                                          namespace TestNamespace;
                                                                          partial class ActivityContextPassedWithStringTagLeft
                                                                          {
                                                                              private static ActivitySource MyActivitySource = new("My");
                                                                              private static partial Activity? StartMyActivity(
                                                                                  string tag,
                                                                                  System.Diagnostics.ActivityContext context
                                                                              )
                                                                              {
                                                                                  return MyActivitySource.StartActivity(
                                                                                      "My",
                                                                                      ActivityKind.Internal,
                                                                                      parentContext: context,
                                                                                      tags: new[] {
                                                                                          new KeyValuePair<string, object?>("tag", tag),
                                                                                      }
                                                                                  );
                                                                              }
                                                                          }

                                                                          """;


    [Fact]
    public void PassingActivityContextWithTagOnLeftSideWillFillParentContext()
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(ActivityContextPassedWithStringTagLeftCode, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat("ActivityContextPassedWithStringTagLeft", ".", "My", ".g.cs"))
        );
        Assert.Equal(ActivityContextPassedWithStringTagLeftExpected, classOutput, ignoreLineEndingDifferences: true);
    }
}