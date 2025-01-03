﻿using Microsoft.CodeAnalysis;
using Xunit;

namespace X39.Roslyn.OpenTelemetry.Tests;

public class ActivitySourceDiscoveryTests : CompilationTestBaseClass
{
    private const string ActivitySourceGeneratedCode = """
                                                       using System.Diagnostics;
                                                       using X39.Roslyn.OpenTelemetry.Attributes;
                                                       namespace TestNamespace;

                                                       public partial class ActivitySourceGenerated
                                                       {
                                                           [Activity(ActivityKind.Internal, CreateActivitySource = true)]
                                                           private static partial Activity? StartMyActivity(
                                                               string tag
                                                           );
                                                       }
                                                       """;

    private const string ActivitySourceGeneratedExpected = """
                                                           // <auto-generated/>
                                                           #nullable enable
                                                           using System.Diagnostics;
                                                           using System.ComponentModel;
                                                           using System.Collections.Generic;

                                                           namespace TestNamespace;
                                                           partial class ActivitySourceGenerated
                                                           {
                                                               private static ActivitySource MyActivitySource = new("My");
                                                               private static partial Activity? StartMyActivity(
                                                                   string tag
                                                               )
                                                               {
                                                                   return MyActivitySource.StartActivity(
                                                                       "My",
                                                                       ActivityKind.Internal,
                                                                       parentContext: default,
                                                                       tags: new[] {
                                                                           new KeyValuePair<string, object?>("tag", tag),
                                                                       }
                                                                   );
                                                               }
                                                           }

                                                           """;

    private const string ActivitySourceReferencedOnClassCode = """
                                                               using System.Diagnostics;
                                                               using X39.Roslyn.OpenTelemetry.Attributes;

                                                               namespace OtherFancyAssembly
                                                               {
                                                                   public static class Statics
                                                                   {
                                                                       public static ActivitySource ClassActivitySource = new("My");
                                                                   }
                                                               }

                                                               namespace TestNamespace
                                                               {
                                                                   [ActivitySourceReference("OtherFancyAssembly.Statics.ClassActivitySource")]
                                                                   public partial class ActivitySourceReferencedOnClass
                                                                   {
                                                                       [Activity(ActivityKind.Internal)]
                                                                       private static partial Activity? StartMyActivity(
                                                                           string tag
                                                                       );
                                                                   }
                                                               }
                                                               """;

    private const string ActivitySourceReferencedOnClassExpected = """
                                                                   // <auto-generated/>
                                                                   #nullable enable
                                                                   using System.Diagnostics;
                                                                   using System.ComponentModel;
                                                                   using System.Collections.Generic;

                                                                   namespace TestNamespace;
                                                                   partial class ActivitySourceReferencedOnClass
                                                                   {
                                                                       private static partial Activity? StartMyActivity(
                                                                           string tag
                                                                       )
                                                                       {
                                                                           return OtherFancyAssembly.Statics.ClassActivitySource.StartActivity(
                                                                               "My",
                                                                               ActivityKind.Internal,
                                                                               parentContext: default,
                                                                               tags: new[] {
                                                                                   new KeyValuePair<string, object?>("tag", tag),
                                                                               }
                                                                           );
                                                                       }
                                                                   }

                                                                   """;

    private const string ActivitySourceReferencedOnAssemblyCode = """
                                                               using System.Diagnostics;
                                                               using X39.Roslyn.OpenTelemetry.Attributes;
                                                               [assembly: ActivitySourceReference("OtherFancyAssembly.Statics.ClassActivitySource")]
                                                               namespace OtherFancyAssembly
                                                               {
                                                                   public static class Statics
                                                                   {
                                                                       public static ActivitySource ClassActivitySource = new("My");
                                                                   }
                                                               }

                                                               namespace TestNamespace
                                                               {
                                                                   public partial class ActivitySourceReferencedOnAssembly
                                                                   {
                                                                       [Activity(ActivityKind.Internal)]
                                                                       private static partial Activity? StartMyActivity(
                                                                           string tag
                                                                       );
                                                                   }
                                                               }
                                                               """;

    private const string ActivitySourceReferencedOnAssemblyExpected = """
                                                                      // <auto-generated/>
                                                                      #nullable enable
                                                                      using System.Diagnostics;
                                                                      using System.ComponentModel;
                                                                      using System.Collections.Generic;

                                                                      namespace TestNamespace;
                                                                      partial class ActivitySourceReferencedOnAssembly
                                                                      {
                                                                          private static partial Activity? StartMyActivity(
                                                                              string tag
                                                                          )
                                                                          {
                                                                              return OtherFancyAssembly.Statics.ClassActivitySource.StartActivity(
                                                                                  "My",
                                                                                  ActivityKind.Internal,
                                                                                  parentContext: default,
                                                                                  tags: new[] {
                                                                                      new KeyValuePair<string, object?>("tag", tag),
                                                                                  }
                                                                              );
                                                                          }
                                                                      }

                                                                      """;

    private const string ActivitySourceReferencedOnClassAndMethodCode = """
                                                                        using System.Diagnostics;
                                                                        using X39.Roslyn.OpenTelemetry.Attributes;

                                                                        namespace SomeFancyAssembly
                                                                        {
                                                                            public static class Statics
                                                                            {
                                                                                public static ActivitySource ActualActivitySource = new("My");
                                                                            }
                                                                        }

                                                                        namespace TestNamespace
                                                                        {
                                                                            [ActivitySourceReference("OtherFancyAssembly.ClassActivitySource")]
                                                                            public partial class ActivitySourceReferencedOnClassAndMethod
                                                                            {
                                                                                [ActivitySourceReference("SomeFancyAssembly.Statics.ActualActivitySource")]
                                                                                [Activity(ActivityKind.Internal)]
                                                                                private static partial Activity? StartMyActivity(
                                                                                    string tag
                                                                                );
                                                                            }
                                                                        }
                                                                        """;

    private const string ActivitySourceReferencedOnClassAndMethodExpected = """
                                                                            // <auto-generated/>
                                                                            #nullable enable
                                                                            using System.Diagnostics;
                                                                            using System.ComponentModel;
                                                                            using System.Collections.Generic;

                                                                            namespace TestNamespace;
                                                                            partial class ActivitySourceReferencedOnClassAndMethod
                                                                            {
                                                                                private static partial Activity? StartMyActivity(
                                                                                    string tag
                                                                                )
                                                                                {
                                                                                    return SomeFancyAssembly.Statics.ActualActivitySource.StartActivity(
                                                                                        "My",
                                                                                        ActivityKind.Internal,
                                                                                        parentContext: default,
                                                                                        tags: new[] {
                                                                                            new KeyValuePair<string, object?>("tag", tag),
                                                                                        }
                                                                                    );
                                                                                }
                                                                            }

                                                                            """;

    private const string ActivitySourceGeneratedWithClassAndMethodReferenceCode = """
        using System.Diagnostics;
        using X39.Roslyn.OpenTelemetry.Attributes;

        namespace TestNamespace;
        [ActivitySourceReference("OtherFancyAssembly.ClassActivitySource")]
        public partial class ActivitySourceGeneratedWithClassAndMethodReference
        {
            [ActivitySourceReference("SomeFancyAssembly.Statics.ActualActivitySource")]
            [Activity(ActivityKind.Internal, CreateActivitySource = true)]
            private static partial Activity? StartMyActivity(
                string tag
            );
        }
        """;

    private const string ActivitySourceGeneratedWithClassAndMethodReferenceExpected = """
        // <auto-generated/>
        #nullable enable
        using System.Diagnostics;
        using System.ComponentModel;
        using System.Collections.Generic;

        namespace TestNamespace;
        partial class ActivitySourceGeneratedWithClassAndMethodReference
        {
            private static ActivitySource MyActivitySource = new("My");
            private static partial Activity? StartMyActivity(
                string tag
            )
            {
                return MyActivitySource.StartActivity(
                    "My",
                    ActivityKind.Internal,
                    parentContext: default,
                    tags: new[] {
                        new KeyValuePair<string, object?>("tag", tag),
                    }
                );
            }
        }

        """;

    private const string StaticFieldAvailableNoReferenceAttributesCode = """
                                                                         using System.Diagnostics;
                                                                         using X39.Roslyn.OpenTelemetry.Attributes;

                                                                         namespace TestNamespace;
                                                                         public partial class StaticFieldAvailableNoReferenceAttributes
                                                                         {
                                                                             private static ActivitySource StaticFieldActivitySource = new("My");
                                                                             [Activity(ActivityKind.Internal)]
                                                                             private static partial Activity? StartMyActivity(
                                                                                 string tag
                                                                             );
                                                                         }
                                                                         """;

    private const string StaticFieldAvailableNoReferenceAttributesExpected = """
                                                                             // <auto-generated/>
                                                                             #nullable enable
                                                                             using System.Diagnostics;
                                                                             using System.ComponentModel;
                                                                             using System.Collections.Generic;

                                                                             namespace TestNamespace;
                                                                             partial class StaticFieldAvailableNoReferenceAttributes
                                                                             {
                                                                                 private static partial Activity? StartMyActivity(
                                                                                     string tag
                                                                                 )
                                                                                 {
                                                                                     return StaticFieldActivitySource.StartActivity(
                                                                                         "My",
                                                                                         ActivityKind.Internal,
                                                                                         parentContext: default,
                                                                                         tags: new[] {
                                                                                             new KeyValuePair<string, object?>("tag", tag),
                                                                                         }
                                                                                     );
                                                                                 }
                                                                             }

                                                                             """;

    private const string InstanceFieldAvailableNoReferenceAttributesCode = """
                                                                           using System.Diagnostics;
                                                                           using X39.Roslyn.OpenTelemetry.Attributes;

                                                                           namespace TestNamespace;
                                                                           public partial class InstanceFieldAvailableNoReferenceAttributes
                                                                           {
                                                                               private ActivitySource StaticFieldActivitySource = new("My");
                                                                               [Activity(ActivityKind.Internal)]
                                                                               private partial Activity? StartMyActivity(
                                                                                   string tag
                                                                               );
                                                                           }
                                                                           """;

    private const string InstanceFieldAvailableNoReferenceAttributesExpected = """
                                                                               // <auto-generated/>
                                                                               #nullable enable
                                                                               using System.Diagnostics;
                                                                               using System.ComponentModel;
                                                                               using System.Collections.Generic;

                                                                               namespace TestNamespace;
                                                                               partial class InstanceFieldAvailableNoReferenceAttributes
                                                                               {
                                                                                   private partial Activity? StartMyActivity(
                                                                                       string tag
                                                                                   )
                                                                                   {
                                                                                       return StaticFieldActivitySource.StartActivity(
                                                                                           "My",
                                                                                           ActivityKind.Internal,
                                                                                           parentContext: default,
                                                                                           tags: new[] {
                                                                                               new KeyValuePair<string, object?>("tag", tag),
                                                                                           }
                                                                                       );
                                                                                   }
                                                                               }

                                                                               """;

    private const string StaticPropertyAvailableNoReferenceAttributesCode = """
                                                                         using System.Diagnostics;
                                                                         using X39.Roslyn.OpenTelemetry.Attributes;

                                                                         namespace TestNamespace;
                                                                         public partial class StaticPropertyAvailableNoReferenceAttributes
                                                                         {
                                                                             private static ActivitySource StaticPropertyActivitySource { get; } = new("My");
                                                                             [Activity(ActivityKind.Internal)]
                                                                             private static partial Activity? StartMyActivity(
                                                                                 string tag
                                                                             );
                                                                         }
                                                                         """;

    private const string StaticPropertyAvailableNoReferenceAttributesExpected = """
                                                                             // <auto-generated/>
                                                                             #nullable enable
                                                                             using System.Diagnostics;
                                                                             using System.ComponentModel;
                                                                             using System.Collections.Generic;

                                                                             namespace TestNamespace;
                                                                             partial class StaticPropertyAvailableNoReferenceAttributes
                                                                             {
                                                                                 private static partial Activity? StartMyActivity(
                                                                                     string tag
                                                                                 )
                                                                                 {
                                                                                     return StaticPropertyActivitySource.StartActivity(
                                                                                         "My",
                                                                                         ActivityKind.Internal,
                                                                                         parentContext: default,
                                                                                         tags: new[] {
                                                                                             new KeyValuePair<string, object?>("tag", tag),
                                                                                         }
                                                                                     );
                                                                                 }
                                                                             }

                                                                             """;

    private const string InstancePropertyAvailableNoReferenceAttributesCode = """
                                                                           using System.Diagnostics;
                                                                           using X39.Roslyn.OpenTelemetry.Attributes;

                                                                           namespace TestNamespace;
                                                                           public partial class InstancePropertyAvailableNoReferenceAttributes
                                                                           {
                                                                               private ActivitySource StaticPropertyActivitySource { get; } = new("My");
                                                                               [Activity(ActivityKind.Internal)]
                                                                               private partial Activity? StartMyActivity(
                                                                                   string tag
                                                                               );
                                                                           }
                                                                           """;

    private const string InstancePropertyAvailableNoReferenceAttributesExpected = """
                                                                               // <auto-generated/>
                                                                               #nullable enable
                                                                               using System.Diagnostics;
                                                                               using System.ComponentModel;
                                                                               using System.Collections.Generic;

                                                                               namespace TestNamespace;
                                                                               partial class InstancePropertyAvailableNoReferenceAttributes
                                                                               {
                                                                                   private partial Activity? StartMyActivity(
                                                                                       string tag
                                                                                   )
                                                                                   {
                                                                                       return StaticPropertyActivitySource.StartActivity(
                                                                                           "My",
                                                                                           ActivityKind.Internal,
                                                                                           parentContext: default,
                                                                                           tags: new[] {
                                                                                               new KeyValuePair<string, object?>("tag", tag),
                                                                                           }
                                                                                       );
                                                                                   }
                                                                               }

                                                                               """;

    // @formatter:max_line_length 20000
    [Theory]
    [InlineData("StaticFieldAvailableNoReferenceAttributes", StaticFieldAvailableNoReferenceAttributesCode, StaticFieldAvailableNoReferenceAttributesExpected)]
    [InlineData("InstanceFieldAvailableNoReferenceAttributes", InstanceFieldAvailableNoReferenceAttributesCode, InstanceFieldAvailableNoReferenceAttributesExpected)]
    [InlineData("StaticPropertyAvailableNoReferenceAttributes", StaticPropertyAvailableNoReferenceAttributesCode, StaticPropertyAvailableNoReferenceAttributesExpected)]
    [InlineData("InstancePropertyAvailableNoReferenceAttributes", InstancePropertyAvailableNoReferenceAttributesCode, InstancePropertyAvailableNoReferenceAttributesExpected)]
    [InlineData("ActivitySourceGenerated", ActivitySourceGeneratedCode, ActivitySourceGeneratedExpected)]
    [InlineData("ActivitySourceReferencedOnAssembly", ActivitySourceReferencedOnAssemblyCode, ActivitySourceReferencedOnAssemblyExpected)]
    [InlineData("ActivitySourceReferencedOnClass", ActivitySourceReferencedOnClassCode, ActivitySourceReferencedOnClassExpected)]
    [InlineData("ActivitySourceReferencedOnClassAndMethod", ActivitySourceReferencedOnClassAndMethodCode, ActivitySourceReferencedOnClassAndMethodExpected)]
    [InlineData("ActivitySourceGeneratedWithClassAndMethodReference", ActivitySourceGeneratedWithClassAndMethodReferenceCode, ActivitySourceGeneratedWithClassAndMethodReferenceExpected)]
    // @formatter:max_line_length restore
    public void TestCodeIsAsExpected(string className, string code, string expected)
    {
        var generatedFiles = AssertCompilationAndGetGeneratedFiles(code, []);
        // Complex generators should be tested using text comparison.
        var (_, classOutput) = Assert.Single(
            generatedFiles,
            f => f.FilePath.EndsWith(string.Concat(className, ".", "My", ".g.cs"))
        );
        Assert.Equal(expected, classOutput, ignoreLineEndingDifferences: true);
    }
}