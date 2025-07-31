using System.Collections.Immutable;
using System.Text;
using DotWrap.Generator.Builders;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Builders.Method;
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
                var (compilation, classes) = source;

                // collect all assembly attributes that are marked for external exposure
                var assemblyAttrs = compilation.Assembly.GetAttributes();

                // var externalExposes = assemblyAttrs
                //     .Where(a => a.AttributeClass?.Name == nameof(DotWrapExternalExposeAttribute))
                //     .Select(a => DotWrapExternalExposeAttribute.FromAttributeData(a))
                //     .Select(a => new DotWrapExposeData(a.typeToWrap, a.alias))
                //     .ToList();

                HashSet<ITypeSymbol> allExplicitTypes = [];
                HashSet<ITypeSymbol> allInferedTypes = [];
                List<ITypeSymbol> inferedTypesToWrap = [];
                GlobalContext globalContext = new(
                    allExplicitTypes,
                    allInferedTypes,
                    inferedTypesToWrap
                );

                // System.Diagnostics.Debugger.Launch();

                foreach (var classDecl in classes)
                {
                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    var namedTypeSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (namedTypeSymbol == null)
                    {
                        continue;
                    }

                    if (namedTypeSymbol.GetDotWrapExposeAttribute() is not AttributeData exposeAttr)
                    {
                        // if the class is not marked for wrapper generation, skip it
                        continue;
                    }
                    var classSymbol = DotWrapExposeData.FromAttributeData(
                        namedTypeSymbol,
                        exposeAttr
                    );

                    allExplicitTypes.Add(classSymbol.typeSymbol);
                    var context = new ClassBuilderContext(
                        globalContext,
                        classSymbol.typeSymbol,
                        classSymbol.Alias
                    );
                    string sourceText = new ExplicitWrapperBuilder(context).GenerateClassFile();

                    spc.AddSource(
                        $"{context.WrapperName.Replace("<", "_").Replace(">", "_")}.g.cs",
                        SourceText.From(sourceText, Encoding.UTF8)
                    );
                }

                if (inferedTypesToWrap.Count == 0)
                {
                    return;
                }

                var externalMethodExposes = assemblyAttrs
                    .Where(a => a.AttributeClass?.Name == nameof(DotWrapExternalMethodMeta))
                    .Select(a => DotWrapExternalMethodMeta.FromAttributeData(a))
                    .Concat(
                        assemblyAttrs
                            .Where(a =>
                                a.AttributeClass?.Name == nameof(DotWrapExternalPropertyMeta)
                            )
                            .SelectMany(a => DotWrapExternalPropertyMeta.FromAttributeData(a))
                    )
                    .ToList();

                while (inferedTypesToWrap.Count > 0)
                {
                    for (int i = inferedTypesToWrap.Count - 1; i >= 0; i--)
                    {
                        var classSymbol = inferedTypesToWrap[i];
                        inferedTypesToWrap.RemoveAt(i);
                        if (classSymbol is not INamedTypeSymbol namedTypeSymbol)
                        {
                            continue;
                        }
                        var context = new ClassBuilderContext(globalContext, namedTypeSymbol);
                        string sourceText = new ImplicitWrapperBuilder(
                            context,
                            externalMethodExposes
                        ).GenerateClassFile();

                        spc.AddSource(
                            $"{context.WrapperName.Replace("<", "_").Replace(">", "_")}.g.cs",
                            SourceText.From(sourceText, Encoding.UTF8)
                        );
                    }
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
}

public record DotWrapExposeData(INamedTypeSymbol typeSymbol, string? Alias = null)
{
    public static DotWrapExposeData FromAttributeData(
        INamedTypeSymbol typeSymbol,
        AttributeData attribute
    )
    {
        return new(
            typeSymbol,
            attribute.GetCtorArg<string?>(0, nameof(DotWrap.DotWrapExposeAttribute.alias))
        );
    }
}

public record DotWrapExternalMethodMeta(
    ITypeSymbol containingType,
    string methodName,
    ImmutableArray<TypedConstant>? parameters = null,
    string? alias = null
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
            attribute.GetCtorArg<string>(3, nameof(DotWrap.DotWrapExternalMethodMeta.alias))
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
        if (propertyType is PropertyType.None)
        {
            yield break;
        }
        if (propertyType.HasFlag(PropertyType.Get))
        {
            yield return new(
                attribute.GetCtorArg<ITypeSymbol>(
                    0,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.containingType)
                ),
                $"get_{attribute.GetCtorArg<string>(
                    1,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.propertyName)
                )}",
                alias: attribute.GetCtorArg<string>(
                    4,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.alias)
                )
            );
        }
        if (propertyType.HasFlag(PropertyType.Set))
        {
            yield return new(
                attribute.GetCtorArg<ITypeSymbol>(
                    0,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.containingType)
                ),
                $"set_{attribute.GetCtorArg<string>(
                    1,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.propertyName)
                )}",
                alias: attribute.GetCtorArg<string>(
                    4,
                    nameof(DotWrap.DotWrapExternalPropertyMeta.alias)
                )
            );
        }
    }
}

public record DotWrapExternalExposeAttribute(INamedTypeSymbol typeToWrap, string? alias = null)
{
    public static DotWrapExternalExposeAttribute FromAttributeData(AttributeData attribute)
    {
        return new(
            attribute.GetCtorArg<INamedTypeSymbol>(
                0,
                nameof(DotWrap.DotWrapExternalExposeAttribute.typeToWrap)
            ),
            attribute.GetCtorArg<string?>(1, nameof(DotWrap.DotWrapExternalExposeAttribute.alias))
        );
    }
}
