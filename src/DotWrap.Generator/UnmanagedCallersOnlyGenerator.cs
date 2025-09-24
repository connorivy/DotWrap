using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using DotWrap.Generator.Builders;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Builders.Method;
using DotWrap.Generator.Configuration;
using DotWrap.Generator.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator;

[Generator]
public class UnmanagedCallersOnlyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node
            )
            .Where(static c => c is not null);

        var compilationAndClasses = context.CompilationProvider.Combine(
            classDeclarations.Collect()
        );

        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) =>
            {
                Logger.Context = spc;
                try
                {
                    GenerateUnmanagedOnlyEntryPoints(spc, source);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        );

        context.RegisterPostInitializationOutput(static spc =>
        {
            // This is where you can add additional files to the project, like
            // a file containing the generated code for the CString class.
            var sourceText = SourceText.From(
                $$"""
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using DotWrap;
using DotWrap.Configuration;

[assembly: DotWrapExternalIndexerMeta(typeof(IList<>))]
[assembly: DotWrapExternalMethodMeta(typeof(IList<>), nameof(IList<int>.Add))]
[assembly: DotWrapExternalMethodMeta(typeof(IList<>), nameof(IList<int>.Remove))]

[assembly: DotWrapExternalPropertyMeta(typeof(ICollection<>), nameof(ICollection<int>.Count))]
[assembly: DotWrapExternalPropertyMeta(typeof(IReadOnlyCollection<>), nameof(IReadOnlyCollection<int>.Count))]

[assembly: DotWrapExternalPropertyMeta(typeof(IDictionary<,>), nameof(IDictionary<int, int>.Keys))]
[assembly: DotWrapExternalPropertyMeta(typeof(KeyValuePair<,>), nameof(KeyValuePair<int, int>.Key))]
[assembly: DotWrapExternalPropertyMeta(typeof(KeyValuePair<,>), nameof(KeyValuePair<int, int>.Value))]

[assembly: DotWrapExternalPropertyMeta(typeof(System.Array), nameof(System.Array.Length))]
[assembly: DotWrapExternalMethodMeta(typeof(System.Array), "Add", ignore: true)]
[assembly: DotWrapExternalMethodMeta(typeof(System.Array), "Remove", ignore: true)]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Array), "Count", PropertyType.None)]
[assembly: DotWrapExternalTypeConfig(typeof(System.Array), namespaceAlias: "System.Collections.Generic")]

[assembly: DotWrapExternalPropertyMeta(typeof(Task<>), nameof(Task<int>.Result))]
[assembly: DotWrapExternalPropertyMeta(typeof(Task), nameof(Task.Status))]
[assembly: DotWrapExternalPropertyMeta(typeof(ValueTask<>), nameof(ValueTask<int>.Result))]
[assembly: DotWrapExternalPropertyMeta(typeof(ValueTask<>), nameof(ValueTask<int>.IsFaulted))]
[assembly: DotWrapExternalPropertyMeta(
    typeof(ValueTask<>),
    nameof(ValueTask<int>.IsCompletedSuccessfully)
)]

[assembly: DotWrapExternalMethodMeta(typeof(System.Nullable<>), ".ctor", [typeof(AnyType)])]
[assembly: DotWrapExternalMethodMeta(typeof(System.Nullable<>), ".ctor", [])]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Nullable<>), nameof(Nullable<int>.HasValue))]
[assembly: DotWrapExternalPropertyMeta(typeof(System.Nullable<>), nameof(Nullable<int>.Value))]

namespace DotWrap.BuiltIn
{
    internal static class CString
    {
        public static IntPtr Create(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return IntPtr.Zero;
            }

            var ptr = Marshal.StringToHGlobalAnsi(str);
            return ptr;
        }

        [UnmanagedCallersOnly(EntryPoint = "DotWrap_BuiltIn_CString_Free")]
        public static void Free(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            Marshal.FreeHGlobal(ptr);
        }
    }
}
""",
                Encoding.UTF8
            );
            spc.AddSource("DotWrap.BuiltIn.CString.g.cs", sourceText);
        });
    }

    private static bool GenerateUnmanagedOnlyEntryPoints(
        SourceProductionContext spc,
        (Compilation Left, ImmutableArray<ClassDeclarationSyntax> Right) source
    )
    {
        var (compilation, classes) = source;

        // collect all assembly attributes that are marked for external exposure
        var assemblyAttrs = compilation.Assembly.GetAttributes();
        var currentAssembly = compilation.Assembly;

        HashSet<INamedTypeSymbol> allExplicitTypes = [];
        HashSet<ITypeSymbol> allInferedTypes = [];
        Queue<ITypeSymbol> inferedTypesToWrap = [];
        Queue<INamedTypeSymbol> explicitTypesToWrap = [];
        var exposeEntireAssembly = assemblyAttrs.Any(a =>
            a.AttributeClass?.Name == nameof(DotWrap.DotWrapExposeAssemblyAttribute)
        );

        var assembliesToExpose = assemblyAttrs
            .Where(a =>
                a.AttributeClass?.Name == nameof(DotWrap.DotWrapExposeAssemblyAttribute)
            )
            .Select(a => a.GetCtorArg<ITypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExposeAssemblyAttribute.assemblyType)
            ))
            .Select(t => t?.ContainingAssembly ?? currentAssembly)
            .ToImmutableHashSet<IAssemblySymbol>(SymbolEqualityComparer.Default);

        GlobalContext globalContext = new(allExplicitTypes, allInferedTypes, inferedTypesToWrap, explicitTypesToWrap, assembliesToExpose);

        // System.Diagnostics.Debugger.Launch();
        foreach (var classDecl in classes)
        {
            Logger.LogInfo($"Processing class declaration: {classDecl.Identifier.Text}");
            var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
            var namedTypeSymbol = semanticModel.GetDeclaredSymbol(classDecl);
            if (namedTypeSymbol == null)
            {
                continue;
            }

            var isPublic = namedTypeSymbol.DeclaredAccessibility == Accessibility.Public;
            if (!(isPublic && exposeEntireAssembly) && namedTypeSymbol.GetDotWrapExposeAttribute() is not AttributeData exposeAttr)
            {
                // if the class is not marked for wrapper generation, skip it
                continue;
            }
            globalContext.AddDiscoveredType(namedTypeSymbol);
        }

        var externallyExposedTypeMeta = assemblyAttrs
            .Where(a =>
                a.AttributeClass?.Name == nameof(DotWrap.DotWrapExternalTypeConfigAttribute)
            )
            .Select(a => DotWrapExternalExposeAttribute.FromAttributeData(a))
            .OrderByDescending(meta =>
                (
                    meta.TypeWithMetadata is INamedTypeSymbol namedTypeSymbol
                        ? namedTypeSymbol.TypeParameters.Length
                        : 0
                )
            )
            .ToList();

        var externalMethodExposes = assemblyAttrs
            .Where(a => a.AttributeClass?.Name == nameof(DotWrap.DotWrapExternalMethodMeta))
            .Select(a => DotWrapExternalMethodMeta.FromAttributeData(a))
            .Concat(
                assemblyAttrs
                    .Where(a =>
                        a.AttributeClass?.Name == nameof(DotWrap.DotWrapExternalPropertyMeta)
                    )
                    .SelectMany(a => DotWrapExternalPropertyMeta.FromAttributeData(a))
            )
            .Concat(
                assemblyAttrs
                    .Where(a =>
                        a.AttributeClass?.Name == nameof(DotWrap.DotWrapExternalIndexerMeta)
                    )
                    .SelectMany(a => DotWrapExternalIndexerMeta.FromAttributeData(a))
            )
            .ToList();

        while (inferedTypesToWrap.Count > 0 || explicitTypesToWrap.Count > 0)
        {
            while (explicitTypesToWrap.Count > 0 && explicitTypesToWrap.Dequeue() is INamedTypeSymbol namedTypeSymbol)
            {
                DotWrapExposeAttribute exposeAttribute;
                if (namedTypeSymbol.GetDotWrapExposeAttribute() is not AttributeData exposeAttr)
                {
                    exposeAttribute = new DotWrapExposeAttribute();
                }
                else
                {
                    exposeAttribute = DotWrapExposeData.FromAttributeData(exposeAttr);
                }

                allExplicitTypes.Add(namedTypeSymbol);
                var context = new ClassBuilderContext(globalContext, namedTypeSymbol, exposeAttribute);
                string sourceText = new ExplicitWrapperBuilder(context).GenerateClassFile();

                spc.AddSource(
                    $"{context.WrapperName}.g.cs",
                    SourceText.From(sourceText, Encoding.UTF8)
                );
            }

            while (inferedTypesToWrap.Count > 0 && inferedTypesToWrap.Dequeue() is ITypeSymbol classSymbol)
            {
                // todo: better selection than just taking the first matching type that we find
                var typeMetadata = externallyExposedTypeMeta.FirstOrDefault(meta =>
                    classSymbol.HasInheritanceOrImplementationRelationship(meta.TypeWithMetadata)
                );

                var context = new ClassBuilderContext(
                    globalContext,
                    classSymbol,
                    typeMetadata ?? new DotWrapExposeAttribute()
                );
                string sourceText = new ImplicitWrapperBuilder(
                    context,
                    externalMethodExposes
                ).GenerateClassFile();

                spc.AddSource(
                    $"{context.WrapperName}.g.cs",
                    SourceText.From(sourceText, Encoding.UTF8)
                );
            }
        }

        return true;
    }
}

public record DotWrapExposeData
{
    public static DotWrapExposeAttribute FromAttributeData(AttributeData attribute)
    {
        return new(
            attribute.GetCtorArg<string?>(0, nameof(DotWrap.DotWrapExposeAttribute.alias)),
            attribute.GetCtorArg<string?>(1, nameof(DotWrap.DotWrapExposeAttribute.namespaceAlias))
        );
    }
}

public record DotWrapExternalMethodMeta(
    ITypeSymbol containingType,
    string methodName,
    ImmutableArray<TypedConstant>? parameters = null,
    string? alias = null,
    bool ignore = false
)
{
    public static DotWrapExternalMethodMeta FromAttributeData(AttributeData attribute)
    {
        return new(
            attribute.GetCtorArg<ITypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExternalMethodMeta.containingType)
            ),
            attribute.GetCtorArg<string>(1, nameof(DotWrap.DotWrapExternalMethodMeta.methodName)),
            attribute.GetCtorArgForCollection<ImmutableArray<TypedConstant>?>(
                2,
                nameof(DotWrap.DotWrapExternalMethodMeta.parameters)
            ),
            attribute.GetCtorArg<string>(3, nameof(DotWrap.DotWrapExternalMethodMeta.alias)),
            attribute.GetCtorArg<bool>(4, nameof(DotWrap.DotWrapExternalMethodMeta.ignore))
        );
    }
}

public record DotWrapExternalPropertyMeta(
    ITypeSymbol containingType,
    string propertyName,
    PropertyType? propertyType,
    ImmutableArray<TypedConstant>? parameters = null,
    string? alias = null
)
{
    public static IEnumerable<DotWrapExternalMethodMeta> FromAttributeData(AttributeData attribute)
    {
        var propertyType = attribute.GetCtorArg<PropertyType>(
            2,
            nameof(DotWrap.DotWrapExternalPropertyMeta.propertyType)
        );
        var containingType =
            attribute.GetCtorArg<ITypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExternalPropertyMeta.containingType)
            ) ?? throw new ArgumentException("Containing type cannot be null", nameof(attribute));
        var methodName = attribute.GetCtorArg<string>(
            1,
            nameof(DotWrap.DotWrapExternalPropertyMeta.propertyName)
        );
        var alias = attribute.GetCtorArg<string>(
            3,
            nameof(DotWrap.DotWrapExternalPropertyMeta.alias)
        );

        if (propertyType is PropertyType.None)
        {
            yield return new(containingType, $"get_{methodName}", alias: alias, ignore: true);
            yield return new(containingType, $"set_{methodName}", alias: alias, ignore: true);
            yield break;
        }
        if (propertyType.HasFlag(PropertyType.Get))
        {
            yield return new(containingType, $"get_{methodName}", alias: alias);
        }
        if (propertyType.HasFlag(PropertyType.Set))
        {
            yield return new(containingType, $"set_{methodName}", alias: alias);
        }
    }
}

public record DotWrapExternalIndexerMeta(INamedTypeSymbol typeToWrap, string? alias = null)
{
    public static IEnumerable<DotWrapExternalMethodMeta> FromAttributeData(AttributeData attribute)
    {
        var propertyType = attribute.GetCtorArg<PropertyType>(
            1,
            nameof(DotWrap.DotWrapExternalIndexerMeta.propertyType)
        );
        if (propertyType is PropertyType.None)
        {
            yield break;
        }
        var containingType =
            attribute.GetCtorArg<ITypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExternalIndexerMeta.containingType)
            ) ?? throw new ArgumentException("Containing type cannot be null", nameof(attribute));

        if (propertyType.HasFlag(PropertyType.Get))
        {
            yield return new(
                containingType,
                "get_Item",
                alias: attribute.GetCtorArg<string>(
                    2,
                    nameof(DotWrap.DotWrapExternalIndexerMeta.alias)
                )
            );
        }
        if (propertyType.HasFlag(PropertyType.Set))
        {
            yield return new(
                containingType,
                "set_Item",
                alias: attribute.GetCtorArg<string>(
                    2,
                    nameof(DotWrap.DotWrapExternalIndexerMeta.alias)
                )
            );
        }
    }
}

public class DotWrapExternalExposeAttribute(
    ITypeSymbol typeWithMetadata,
    string? alias = null,
    string? namespaceAlias = null
) : DotWrap.DotWrapExposeAttribute(alias, namespaceAlias)
{
    public ITypeSymbol TypeWithMetadata => typeWithMetadata;

    public static DotWrapExternalExposeAttribute FromAttributeData(AttributeData attribute)
    {
        return new(
            attribute.GetCtorArg<ITypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExternalTypeConfigAttribute.typeWithMetadata)
            ),
            attribute.GetCtorArg<string?>(
                1,
                nameof(DotWrap.DotWrapExternalTypeConfigAttribute.alias)
            ),
            attribute.GetCtorArg<string?>(
                2,
                nameof(DotWrap.DotWrapExternalTypeConfigAttribute.namespaceAlias)
            )
        );
    }
}
