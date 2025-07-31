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
        if (classSymbol.TypeKind is TypeKind.Array)
        {
            applicableNamedTypeSymbols = applicableNamedTypeSymbols.Append(
                classSymbol.BaseType
                    ?? throw new InvalidOperationException("Array type must have a base type.")
            );
        }

        var applicableExposes = GetExternalMethodExposeContext(Context, externalMethodMeta)
            .ToList();

        if (applicableExposes.Count == 0)
        {
            return;
        }

        HashSet<IMethodSymbol> visitedMethodSymbols = [];
        foreach (var exposeContext in applicableExposes)
        {
            IMethodSymbol? methodSymbol = null;
            foreach (
                var namedTypeSymbol in applicableNamedTypeSymbols.OrderByDescending(sym =>
                    sym is INamedTypeSymbol namedType ? namedType.TypeArguments.Length : 0
                )
            )
            {
                methodSymbol = namedTypeSymbol
                    .GetMembers(exposeContext.methodName)
                    .OfType<IMethodSymbol>()
                    .OrderByDescending(m => m.TypeParameters.Length)
                    .FirstOrDefault();

                if (methodSymbol is not null)
                {
                    break; // Found a matching method symbol, exit the loop
                }
            }
            // var methodSymbols = classSymbol
            //     .GetMembers(exposeContext.methodName)
            //     // .Where(m =>
            //     // {
            //     //     if (exposeContext.Parameters is null)
            //     //     {
            //     //         return true;
            //     //     }
            //     //     return exposeContext.Parameters.Length == m.P
            //     // })
            //     .OfType<IMethodSymbol>()
            //     .FirstOrDefault();

            if (methodSymbol is null || !visitedMethodSymbols.Add(methodSymbol))
            {
                continue;
            }

            // If we have a method symbol, we can use it to generate the method
            var methodBuilder = new MethodBuilder(methodsSource, classMetadataBuilder, Context);
            methodBuilder.GenerateSingleMethod(Context, methodSymbol);
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
