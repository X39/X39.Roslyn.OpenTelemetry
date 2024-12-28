using System.Diagnostics;

namespace X39.Roslyn.OpenTelemetry.Integration;

public static class Statics
{
    public static readonly ActivitySource ApplicationSource = new("X39.Roslyn.OpenTelemetry.Integration");
}