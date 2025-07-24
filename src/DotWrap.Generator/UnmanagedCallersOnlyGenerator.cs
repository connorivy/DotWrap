using System.Text;
using DotWrap.Generator.Builders.Class;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
                // System.Diagnostics.Debugger.Launch();
                var (compilation, classes) = source;
                foreach (var classDecl in classes)
                {
                    var semanticModel = compilation.GetSemanticModel(classDecl.SyntaxTree);
                    var classSymbol =
                        semanticModel.GetDeclaredSymbol(classDecl) as INamedTypeSymbol;
                    if (classSymbol == null)
                        continue;
                    var hasAttribute = classSymbol
                        .GetAttributes()
                        .Any(a => a.AttributeClass?.Name == nameof(DotWrapExposeAttribute));
                    if (!hasAttribute)
                        continue;

                    var context = new ClassBuilderContext(classSymbol);
                    string sourceText = new EntryPointStaticClassBuilder(context).GenerateClassFile(
                        classSymbol
                    );

                    spc.AddSource(
                        $"{context.WrapperName}.g.cs",
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
                """
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
