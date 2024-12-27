namespace X39.Roslyn.OpenTelemetry.Generator;

internal class Constants
{
    internal const string Namespace                        = "X39.Roslyn.OpenTelemetry.Attributes";
    internal const string ActivityAttribute                = Namespace + "." + "ActivityAttribute";
    internal const string ClientActivityAttribute          = Namespace + "." + "ClientActivityAttribute";
    internal const string ConsumerActivityAttribute        = Namespace + "." + "ConsumerActivityAttribute";
    internal const string InternalActivityAttribute        = Namespace + "." + "InternalActivityAttribute";
    internal const string ProducerActivityAttribute        = Namespace + "." + "ProducerActivityAttribute";
    internal const string ServerActivityAttribute          = Namespace + "." + "ServerActivityAttribute";
    internal const string ActivitySourceReferenceAttribute = Namespace + "." + "ActivitySourceReferenceAttribute";

    public static class CodeGen
    {
        /// <summary>
        /// Diagnostic descriptor prefix for IDs.
        /// It is built to contain the "X39" alias and "OTEL" for "OpenTelemetry".
        /// </summary>
        public const string DiagnosticPrefix = "X39" + "OTEL";

        public const string ActivitySourcePrefix = "";
        public const string ActivitySourceSuffix = "ActivitySource";
    }
}