using System.Collections.Immutable;
using System.Text;
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
                List<DotWrapExposeData> explicitTypesToWrap = [];

                // todo: populate this
                HashSet<INamedTypeSymbol> explicitExternalTypesToWrap = [];
                HashSet<INamedTypeSymbol> inferedTypedToWrap = [];

                // collect all assembly attributes that are marked for external exposure
                var assemblyAttrs = compilation.Assembly.GetAttributes();

                var externalExposes = assemblyAttrs
                    .Where(a => a.AttributeClass?.Name == nameof(DotWrapExternalExposeAttribute))
                    .Select(a => DotWrapExternalExposeAttribute.FromAttributeData(a))
                    .Select(a => new DotWrapExposeData(a.typeToWrap, a.alias))
                    .ToList();

                foreach (var classDecl in classes)
                {
                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);
                    if (classSymbol == null)
                    {
                        continue;
                    }

                    if (classSymbol.GetDotWrapExposeAttribute() is not AttributeData exposeAttr)
                    {
                        // if the class is not marked for wrapper generation, skip it
                        continue;
                    }
                    explicitTypesToWrap.Add(
                        DotWrapExposeData.FromAttributeData(classSymbol, exposeAttr)
                    );
                    inferedTypedToWrap.UnionWith(
                        GetInferredTypesToWrap(classSymbol, explicitExternalTypesToWrap)
                    );
                }

                if (explicitTypesToWrap.Count == 0 && inferedTypedToWrap.Count == 0)
                {
                    return; // no types to wrap, nothing to do
                }
                // System.Diagnostics.Debugger.Launch();

                foreach (var classSymbol in explicitTypesToWrap)
                {
                    var context = new ClassBuilderContext(
                        classSymbol.typeSymbol,
                        classSymbol.Alias
                    );
                    string sourceText = new ExplicitWrapperBuilder(context).GenerateClassFile();

                    spc.AddSource(
                        $"{context.WrapperName.Replace("<", "_").Replace(">", "_")}.g.cs",
                        SourceText.From(sourceText, Encoding.UTF8)
                    );
                }

                var externalMethodExposes = assemblyAttrs
                    .Where(a => a.AttributeClass?.Name == nameof(DotWrapExternalMethodMeta))
                    .Select(a => DotWrapExternalMethodMeta.FromAttributeData(a))
                    .ToList();

                foreach (var classSymbol in inferedTypedToWrap)
                {
                    var context = new ClassBuilderContext(classSymbol);
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

    public static IEnumerable<INamedTypeSymbol> GetInferredTypesToWrap(
        INamedTypeSymbol classSymbol,
        HashSet<INamedTypeSymbol> explicitExternalTypes
    )
    {
        var classContext = new ClassBuilderContext(classSymbol);
        foreach (var method in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (SkipMethod(method))
            {
                continue;
            }

            var methodContext = new MethodBuilderContext(method, classContext);
            foreach (
                var namedTypeSymbol in methodContext
                    .GetParameterDetails()
                    .Select(p => p.OriginalTypeIfDifferent)
                    .Concat([methodContext.MethodSymbol.ReturnType as INamedTypeSymbol])
                    .OfType<INamedTypeSymbol>()
            )
            {
                if (
                    SkipWrapperGeneration(namedTypeSymbol)
                    || explicitExternalTypes.Contains(namedTypeSymbol)
                )
                {
                    continue;
                }

                if (namedTypeSymbol.IsMarkedForWrapperGeneration())
                {
                    // if a type is marked for wrapper gen, then it will be handled by it's own source generator runs
                    continue;
                }

                yield return namedTypeSymbol;
            }
        }
    }

    private static bool SkipWrapperGeneration(INamedTypeSymbol classSymbol)
    {
        return classSymbol switch
        {
            _ when classSymbol.IsBlittable() => true,
            { SpecialType: SpecialType.System_String } => true,
            _ => false,
        };
    }

    private static bool SkipMethod(IMethodSymbol methodSymbol)
    {
        return methodSymbol.DeclaredAccessibility != Accessibility.Public
            || !methodSymbol.IsDefinition
            || methodSymbol.IsExtensionMethod;
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
