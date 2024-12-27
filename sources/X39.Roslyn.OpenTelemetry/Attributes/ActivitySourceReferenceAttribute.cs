namespace X39.Roslyn.OpenTelemetry.Attributes;

/// <summary>
/// An attribute that indicates a reference to an Activity Source.
/// Can be applied to classes or methods to specify the path of the referenced
/// Activity Source. Helps in tracing and telemetry data configuration.
/// </summary>
/// <remarks>
/// The value passed must be a valid trace from this very <see langword="class"/> to the actual property.
/// See examples for better context.
/// </remarks>
/// <example>
/// <code>
/// // Statics.cs
/// namespace My.Assembly;
/// public static class Statics
/// {
///     public static readonly ActivitySource MyActivitySource = new("My");
/// }
/// </code>
/// <code>
/// // MyClass
/// namespace My.Assembly;
///
/// [AcitivitySourceReference("My.Assembly.MyActivitySource");
/// public class MyClass
/// {
///     // ...
/// }
/// </code>
/// </example>
/// <seealso cref="ActivityAttribute"/>
/// <seealso cref="InternalActivityAttribute"/>
/// <seealso cref="ServerActivityAttribute"/>
/// <seealso cref="ClientActivityAttribute"/>
/// <seealso cref="ProducerActivityAttribute"/>
/// <seealso cref="ConsumerActivityAttribute"/>
/// <seealso cref="ActivitySourceReferenceAttribute"/>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ActivitySourceReferenceAttribute(string path) : Attribute;