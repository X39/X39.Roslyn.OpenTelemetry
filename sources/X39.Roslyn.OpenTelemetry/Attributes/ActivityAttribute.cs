using System.Diagnostics;

namespace X39.Roslyn.OpenTelemetry.Attributes;

/// <summary>
/// Represents an attribute that can be applied to a method to instruct a source generator to generate
/// code for starting that activity.
/// </summary>
/// <remarks>
/// This attribute describes the basic activity data for a method.
/// The method this attribute is attached to must always return <see cref="Activity"/>.
/// <list type="bullet">
/// <item>
/// <b>Explicit parent <see cref="ActivityContext"/></b><br/>
/// While the implicit flow of <see cref="Activity"/>'s is "guaranteed", sometimes it is desirable to have the flow
/// explicit. In such cases, you may add a single <see cref="ActivityContext"/> parameter to your method, making the
/// <see cref="Activity"/> returned contain the explicit parent context.
/// </item>
/// <item>
/// <b><see cref="ActivityLink"/>'s</b><br/>
/// To add one or more <see cref="ActivityLink"/>'s to an <see cref="Activity"/>, add either
/// individual <see cref="ActivityLink"/> parameters to your method, or a single <see cref="IEnumerable{T}"/> returning
/// <see cref="ActivityLink"/>'s. If both, singular <see cref="ActivityLink"/>'s and <see cref="IEnumerable{T}"/> with
/// <see cref="ActivityLink"/> are present on a method, an analyzer error will be raised and no implementation be
/// generated.
/// </item>
/// <item>
/// <b><see cref="Activity.Tags"/></b><br/>
/// Adding <see cref="Activity.Tags"/> to an activity is as simple as adding a parameter to your method.
/// Any parameter not used otherwise (e.g. <see cref="ActivityLink"/>), will automatically be added as tag to the activity.
/// </item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// #nullable enable
/// // Most basic setup for activities
/// public partial class MyClass(ActivitySource activitySource)
/// {
///     [InternalActivity]
///     private partial Activity? StartMyActivity();
/// }
/// </code>
/// </example>
/// <example>
/// <code>
/// #nullable enable
/// // Central, static activity class with explicit Activity Context
/// [ActivitySourceReference("Program.MyActivitySource")
/// public static partial class AllActivities
/// {
///     [ServerActivity]
///     public static partial Activity? StartSomeActivity(ActivityContext parentContext);
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
[AttributeUsage(AttributeTargets.Method)]
public class ActivityAttribute(ActivityKind activityKind) : Attribute
{
    /// <summary>
    /// Gets the kind associated with an activity. Represents the type of activity being performed
    /// and is used to determine how the activity will be processed or treated in a distributed
    /// tracing context.
    /// </summary>
    /// <remarks>
    /// For more details, check the individual <see cref="System.Diagnostics.ActivityKind"/> values.
    /// </remarks>
    public ActivityKind ActivityKind { get; } = activityKind;

    /// <summary>
    /// If non-null, overrides the automatic name generation used by the generator.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicates whether the activity associated with this attribute is a root activity.
    /// A root activity typically represents the starting point of a trace in a distributed
    /// tracing context.
    /// </summary>
    /// <remarks>
    /// If this is set to <see langword="true"/>, the activity will be started with a new trace id.
    /// Do note that if any parameter is a <see cref="ActivityContext"/>, this property will be ignored.
    /// </remarks>
    public bool IsRoot { get; set; }

    /// <summary>
    /// Determines whether an <see cref="System.Diagnostics.ActivitySource"/> should be created for the annotated method.
    /// This allows for optional customization of activity source creation in relation to the specified activity attributes.
    /// </summary>
    /// <remarks>
    /// The <see cref="ActivitySource"/> generated will have the same name as the activity (see <see cref="Name"/>).
    /// You usually do not need this as, generally speaking, you only really need one <see cref="ActivitySource"/> per
    /// application, injecting that <see cref="ActivitySource"/> via e.g., dependency injection or referring to it via
    /// <see langword="static"/> property.
    /// <br/>
    /// Do note that this will always overrule the <see cref="ActivitySourceReferenceAttribute"/> setting in all
    /// scenarios!
    /// </remarks>
    public bool CreateActivitySource { get; set; }
}