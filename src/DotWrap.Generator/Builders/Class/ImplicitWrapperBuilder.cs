using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using DotWrap.Generator.Builders.Method;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ImplicitWrapperBuilder(
    ClassBuilderContext context,
    IList<DotWrapExternalMethodMeta> externalMethodMeta
) : EntryPointStaticClassBuilderBase(context)
{
    public override void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    )
    {
        var classSymbol = Context.ClassSymbol;
        IEnumerable<ITypeSymbol> applicableNamedTypeSymbols = [classSymbol];

        // if the symbol is an interface, the only members returned from the GetMembers call will be the members defined
        // on the interface itself. For example, GetMembers call will not return the `get_Count` method of `IList<T>`
        // because it is defined on the `ICollection<T>` interface. Therefore we, if the class symbol is an interface,
        // we need to also include all interfaces that the class symbol implements.
        //
        // if the class symbol is an array, then really nothing is defined on the class symbol itself,
        // the base symbol is `System.Array`, which has several members, but they are all generic and deal with `object`
        // so we also want to include the interfaces for array
        if (classSymbol.TypeKind is TypeKind.Interface or TypeKind.Array)
        {
            applicableNamedTypeSymbols = applicableNamedTypeSymbols.Concat(
                classSymbol.AllInterfaces
            );
        }

        // certain properties, like `Length` are defined on the base type of the array
        var baseSymbol = classSymbol.BaseType;
        while (baseSymbol is not null)
        {
            applicableNamedTypeSymbols = applicableNamedTypeSymbols.Append(baseSymbol);
            baseSymbol = baseSymbol.BaseType;
        }

        // if (classSymbol.TypeKind is TypeKind.Array)
        // {
        //     applicableNamedTypeSymbols = applicableNamedTypeSymbols.Append(
        //         classSymbol.BaseType
        //             ?? throw new InvalidOperationException("Array type must have a base type.")
        //     );
        // }

        var applicableMetaMethods = GetExternalMethodExposeContext(Context, externalMethodMeta)
            .GroupBy(meta => meta.methodName)
            .ToList();

        var applicableNamedTypeSymbolsList = applicableNamedTypeSymbols
            .OrderByDescending(sym =>
                sym is INamedTypeSymbol namedType ? namedType.TypeArguments.Length : 0
            )
            .ToList();

        if (applicableMetaMethods.Count == 0)
        {
            return;
        }

        HashSet<IMethodSymbol> visitedSymbols = new(new NameParamReturnComparer());
        foreach (var metaMethodGroup in applicableMetaMethods)
        {
            if (
                metaMethodGroup.Any(m =>
                    (m.parameters is null || m.parameters == default) && m.ignore
                )
            )
            {
                continue;
            }

            foreach (var methodMeta in metaMethodGroup.GroupBy(m => m.parameters))
            {
                if (methodMeta.Any(m => m.ignore))
                {
                    continue;
                }

                IMethodSymbol? methodSymbol = null;
                foreach (var namedTypeSymbol in applicableNamedTypeSymbolsList)
                {
                    var methodSymbols = namedTypeSymbol
                        .GetMembers(metaMethodGroup.Key)
                        .OfType<IMethodSymbol>()
                        .ToList();

                    methodSymbol = methodSymbols
                        .Where(m =>
                        {
                            if (methodMeta.Key is null || methodMeta.Key == default)
                            {
                                return true;
                            }
                            for (int i = 0; i < methodMeta.Key.Value.Length; i++)
                            {
                                if (
                                    !SymbolEqualityComparer.Default.Equals(
                                        methodMeta.Key.Value[i].Type,
                                        m.Parameters[i].Type
                                    )
                                )
                                {
                                    return false;
                                }
                            }
                            return true;
                        })
                        .OrderByDescending(m => m.TypeParameters.Length)
                        .FirstOrDefault();

                    if (methodSymbol is not null)
                    {
                        // if we found a method symbol, we can stop looking for it in other named type symbols
                        break;
                    }
                }

                if (methodSymbol is null || !visitedSymbols.Add(methodSymbol))
                {
                    continue;
                }

                // If we have a method symbol, we can use it to generate the method
                var methodBuilder = new MethodBuilder(methodsSource, classMetadataBuilder, Context);
                methodBuilder.GenerateSingleMethod(Context, methodSymbol);
            }
        }
    }

    /// <summary>
    /// given a list of external method exposes, find all that apply to the current class symbol.
    /// an external method expose applies if the externalMethodExposes.containingType is assignable to the type of the class symbol.
    /// Conditions to check (example assumes class context symbol is `List<int>`):
    /// - exact type match (List<int>)
    /// - type implements exact interface match (IList<int>)
    /// - type is generic type definition (List<>)
    /// - type is generic type definition and implements interface (IList<>)
    /// </summary>
    /// <param name="classContext"></param>
    /// <param name="externalMethodMetas"></param>
    /// <returns></returns>
    public IEnumerable<DotWrapExternalMethodMeta> GetExternalMethodExposeContext(
        ClassBuilderContext classContext,
        IList<DotWrapExternalMethodMeta> externalMethodMetas
    )
    {
        var classSymbol = classContext.ClassSymbol;
        foreach (var externalMethodMeta in externalMethodMetas)
        {
            var exposeTypeSymbol = externalMethodMeta.containingType;

            // 1. Exact type match for classSymbol or base of classSymbol (e.g., List<int> == List<int>)
            if (CurrentSymOrBaseOfCurrentMatchesCompareSymbol(classSymbol, exposeTypeSymbol))
            {
                yield return externalMethodMeta;
                continue;
            }

            // 2. Type implements exact interface match (e.g., List<int> implements IList<int>)
            if (
                classSymbol.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i, exposeTypeSymbol)
                )
            )
            {
                yield return externalMethodMeta;
                continue;
            }

            var namedExposeTypeSymbol = exposeTypeSymbol as INamedTypeSymbol;
            if (namedExposeTypeSymbol is null)
            {
                continue; // not a named type, skip
            }

            // 3. Type is generic type definition (e.g., List<int> matches List<>)
            if (namedExposeTypeSymbol.IsGenericType)
            {
                if (
                    classSymbol is INamedTypeSymbol namedClassSymbol
                    && namedClassSymbol.IsGenericType
                )
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            namedClassSymbol.ConstructedFrom,
                            namedExposeTypeSymbol.ConstructedFrom
                        )
                    )
                    {
                        yield return externalMethodMeta;
                    }
                }
            }

            // 4. Type is generic type definition and implements interface (e.g., List<int> implements IList<>)
            if (namedExposeTypeSymbol.IsGenericType)
            {
                foreach (var iface in classSymbol.AllInterfaces)
                {
                    if (iface is INamedTypeSymbol namedIface && namedIface.IsGenericType)
                    {
                        if (
                            SymbolEqualityComparer.Default.Equals(
                                namedIface.ConstructedFrom,
                                namedExposeTypeSymbol.ConstructedFrom
                            )
                        )
                        {
                            yield return externalMethodMeta;
                        }
                    }
                }
            }
        }
    }

    private bool CurrentSymOrBaseOfCurrentMatchesCompareSymbol(
        ITypeSymbol currentSymbol,
        ITypeSymbol compareSymbol
    )
    {
        var baseSymbol = currentSymbol.BaseType;
        while (baseSymbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(baseSymbol, compareSymbol))
            {
                return true;
            }
            baseSymbol = baseSymbol.BaseType;
        }
        return false;
    }
}

internal class NameParamReturnComparer : IEqualityComparer<IMethodSymbol>
{
    /// <summary>
    /// Checks if two method symbols are equal based on their name, parameters, and return type.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool Equals(IMethodSymbol? x, IMethodSymbol? y)
    {
        if (x is null && y is null)
        {
            return true;
        }
        if (x is null || y is null)
        {
            return false;
        }
        if (x.Name != y.Name || !SymbolEqualityComparer.Default.Equals(x.ReturnType, y.ReturnType))
        {
            return false;
        }
        if (x.Parameters.Length != y.Parameters.Length)
        {
            return false;
        }
        for (int i = 0; i < x.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(x.Parameters[i].Type, y.Parameters[i].Type))
            {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(IMethodSymbol obj)
    {
        if (obj is null)
        {
            return 0;
        }

        int hash = 17;
        hash = hash * 31 + obj.Name.GetHashCode();
        hash = hash * 31 + obj.ReturnType.GetHashCode();
        foreach (var parameter in obj.Parameters)
        {
            hash = hash * 31 + parameter.Type.GetHashCode();
        }
        return hash;
    }
}
