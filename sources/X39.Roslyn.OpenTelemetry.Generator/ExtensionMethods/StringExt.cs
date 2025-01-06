namespace X39.Roslyn.OpenTelemetry.Generator.ExtensionMethods;

public static class StringExt
{
    public static string ToCSharpString(this string activityName) => activityName.Replace("\"", "\\\"");
}