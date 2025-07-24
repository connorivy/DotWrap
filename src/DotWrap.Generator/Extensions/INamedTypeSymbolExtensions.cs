using System;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class INamedTypeSymbolExtensions
{
    public static bool IsMarkedForWrapperGeneration(this INamedTypeSymbol classSymbol)
    {
        return classSymbol
            .GetAttributes()
            .Any(a => a.AttributeClass?.Name == nameof(DotWrapExposeAttribute));
    }

    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
    /// </summary>
    /// <param name="classSymbol"></param>
    /// <returns></returns>
    public static bool IsBlittable(this INamedTypeSymbol classSymbol) =>
        classSymbol.SpecialType.IsBlittable();
}
