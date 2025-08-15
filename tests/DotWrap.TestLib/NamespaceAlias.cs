using DotWrap;

[assembly: DotWrapExternalTypeConfig(typeof(DayOfWeek), namespaceAlias: "DOW.Namespace.Alias")]

// [assembly: DotWrapExternalTypeConfig(typeof(TypeCode), alias: "TypeCodeAlias")]

[DotWrapExpose]
public static class NamespaceOperations
{
    // public static TypeCode GetColor() => default;

    public static DayOfWeek GetDayOfWeek() => default;
}
