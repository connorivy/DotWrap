using DotWrap;

[assembly: DotWrapExternalExpose(typeof(DayOfWeek), namespaceAlias: "DOW.Namespace.Alias")]
[assembly: DotWrapExternalExpose(typeof(TypeCode), alias: "TypeCodeAlias")]

[DotWrapExpose]
public static class NamespaceOperations
{
    public static TypeCode GetColor() => default;

    public static DayOfWeek GetDayOfWeek() => default;
}
