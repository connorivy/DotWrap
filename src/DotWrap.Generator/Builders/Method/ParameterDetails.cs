using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Method;

public record ParameterDetails(
    string Name,
    string ExposedType,
    ITypeSymbol? OriginalTypeIfDifferent,
    bool IsOutParam,
    bool IsRequiredProperty = false
);
