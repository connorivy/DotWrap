using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class INamedTypeSymbolExtensions
{
    extension(INamedTypeSymbol classSymbol)
    {
        public bool IsMarkedForWrapperGeneration()
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
        public bool IsBlittable() =>
            classSymbol.SpecialType.IsBlittable();
    }
}
