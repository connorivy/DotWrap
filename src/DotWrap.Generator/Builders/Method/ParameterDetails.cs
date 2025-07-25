using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Method;

public record ParameterDetails(
    string Name,
    string ExposedType,
    INamedTypeSymbol? OriginalTypeIfDifferent
);
