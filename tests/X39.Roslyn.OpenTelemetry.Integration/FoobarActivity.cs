using System;
using System.Diagnostics;
using X39.Roslyn.OpenTelemetry.Attributes;

namespace X39.Roslyn.OpenTelemetry.Integration;

public partial class FoobarActivity
{
    [Activity(ActivityKind.Internal, IsRoot = true)]
    private static partial Activity? StartFoobarActivityActivity();
}