using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
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
                Logger.Context = spc;
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
                List<ITypeSymbol> inferedTypes = [];
                GlobalContext globalContext = new(allExplicitTypes, allInferedTypes, inferedTypes);

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

                Logger.LogInfo(
                    $"Found {allExplicitTypes.Count} explicit types for wrapper generation."
                );
                Logger.LogInfo(
                    $"Found {inferedTypes.Count} inferred types for wrapper generation."
                );
                if (inferedTypes.Count == 0)
                {
                    return;
                }

                var externalMethodExposes = assemblyAttrs
                    .Where(a => a.AttributeClass?.Name == nameof(DotWrap.DotWrapExternalMethodMeta))
                    .Select(a => DotWrapExternalMethodMeta.FromAttributeData(a))
                    .Concat(
                        assemblyAttrs
                            .Where(a =>
                                a.AttributeClass?.Name
                                == nameof(DotWrap.DotWrapExternalPropertyMeta)
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

                while (inferedTypes.Count > 0)
                {
                    for (int i = inferedTypes.Count - 1; i >= 0; i--)
                    {
                        var classSymbol = inferedTypes[i];
                        inferedTypes.RemoveAt(i);
                        var context = new ClassBuilderContext(globalContext, classSymbol);
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

                foreach (var exportedEnum in globalContext.ExportedEnums)
                {
                    string enumSourceText =
                        $@"
using System;
using System.Runtime.InteropServices;

namespace {exportedEnum.Namespace}
{{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [global::System.CodeDom.Compiler.GeneratedCode(""DotWrap"", ""1.0.0"")]
    [global::{nameof(DotWrap)}.{nameof(DotWrap.DotWrapGeneratedEnumMetaAttribute).Replace("Attribute", "")}]
    internal static class {exportedEnum.Name}DotWrapMetadata
    {{
#pragma warning disable CS0414 // Field is assigned to but its value is never used
        private static readonly string {DotWrap.Internal.Constants.Metadata} =  
        """"""
        {JsonSerializer.Serialize(
            exportedEnum,
            DotWrapSerializerOptions.Default
        )}
        """""";
#pragma warning restore CS0414 // Field is assigned to but its value is never used;
    }}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}}
";

                    spc.AddSource(
                        $"{exportedEnum.Namespace.Replace(".", "_")}_{exportedEnum.Name}.g.cs",
                        SourceText.From(enumSourceText, Encoding.UTF8)
                    );
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
