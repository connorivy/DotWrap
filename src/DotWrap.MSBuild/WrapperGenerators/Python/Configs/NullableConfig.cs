using DotWrap.Configuration.Python;

internal class NullableConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(Nullable<>);

    public override void ConfigureGenericClassBody(PythonTypeConfigContext context)
    {
        var genericClassBodyBuilder = context.ClassBody;
        genericClassBodyBuilder?.AppendLine(
            $"""
@classmethod
def _create(cls, value: T | None) -> "Nullable[T]":
    if value is None:
        return cls.constructor_1()
    return cls(value)
"""
        );
    }
}
