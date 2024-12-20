using System.Diagnostics;

namespace X39.Roslyn.OpenTelemetry.Attributes;

/// <inheritdoc cref="ActivityAttribute"/>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ClientActivityAttribute() : ActivityAttribute(ActivityKind.Client);