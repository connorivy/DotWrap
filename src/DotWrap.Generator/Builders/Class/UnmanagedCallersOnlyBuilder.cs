using System.Text;
using System.Text.Json;
using DotWrap.Generator.Builders.Method;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Class;

public class EntryPointStaticClassBuilder(ClassBuilderContext context)
{
    public string GenerateClassFile(INamedTypeSymbol classSymbol)
    {
        var methodsSource = BuildClassBody(context);

        var sourceText =
            $@"
using System;
using System.Runtime.InteropServices;

namespace {context.Namespace}
{{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    [global::System.CodeDom.Compiler.GeneratedCode(""DotWrap"", ""1.0.0"")]
    [global::{nameof(DotWrap)}.{nameof(DotWrap.DotWrapGeneratedAttribute).Replace("Attribute", "")}]
    public static class {context.WrapperName}
    {{
{methodsSource.ToString().TrimEnd()}
    }}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}}
";

        return sourceText;
    }

    private StringBuilder BuildClassBody(ClassBuilderContext context)
    {
        var className = context.ClassName;
        var entryPrefix = context.EntryPrefix;

        var methodsSource = new StringBuilder();
        methodsSource.AppendLine(
            $"        private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, {className}> _instances = new();"
        );
        methodsSource.AppendLine($"        private static int _nextId = 1;");
        methodsSource.AppendLine();

        // Create method
        methodsSource.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{Create}\")]"
        );
        methodsSource.AppendLine($"        public static int {Create}()");
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine($"            var {Obj} = new {className}();");
        methodsSource.AppendLine(
            $"            int id = System.Threading.Interlocked.Increment(ref _nextId);"
        );
        methodsSource.AppendLine($"            _instances[id] = {Obj};");
        methodsSource.AppendLine($"            return id;");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        // internal create method
        methodsSource.AppendLine($"        internal static int {Create}({className} {Obj})");
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine(
            $"            int id = System.Threading.Interlocked.Increment(ref _nextId);"
        );
        methodsSource.AppendLine($"            _instances[id] = {Obj};");
        methodsSource.AppendLine($"            return id;");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        // internal get method
        methodsSource.AppendLine($"        internal static {className} {Get}(int id)");
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine($"            if (!_instances.TryGetValue(id, out var {Obj}))");
        methodsSource.AppendLine(
            $"                throw new System.ArgumentException(\"Invalid instance handle: {SelfPointerName}\");"
        );
        methodsSource.AppendLine($"            return {Obj};");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        // Destroy method
        methodsSource.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{Destroy}\")]"
        );
        methodsSource.AppendLine($"        public static void {Destroy}(int {SelfPointerName})");
        methodsSource.AppendLine("        {");
        methodsSource.AppendLine($"            _instances.TryRemove({SelfPointerName}, out _);");
        methodsSource.AppendLine("        }");
        methodsSource.AppendLine();

        ClassMetadataBuilder classMetadataBuilder = new ClassMetadataBuilder(context);
        InstanceMethodBuilder instanceMethodBuilder = new(methodsSource, classMetadataBuilder);
        instanceMethodBuilder.GenerateAllMethods(context);

        var classSymbol = context.ClassSymbol;
        // Public property getters/setters
        foreach (
            var prop in classSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && !p.IsStatic)
        )
        {
            var propType = prop.Type.ToDisplayString();
            var propName = prop.Name;
            // Getter
            if (
                prop.GetMethod != null
                && prop.GetMethod.DeclaredAccessibility == Accessibility.Public
            )
            {
                methodsSource.AppendLine(
                    $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}get_{propName}\")]"
                );
                methodsSource.AppendLine(
                    $"        public static {propType} get_{propName}(int {SelfPointerName})"
                );
                methodsSource.AppendLine("        {");
                methodsSource.AppendLine(
                    $"            if (!_instances.TryGetValue({SelfPointerName}, out var {Obj}))"
                );
                methodsSource.AppendLine(
                    $"                throw new System.ArgumentException(\"Invalid instance handle: {SelfPointerName}\");"
                );
                methodsSource.AppendLine($"            return {Obj}.{propName};");
                methodsSource.AppendLine("        }");
                methodsSource.AppendLine();
            }
            // Setter
            if (
                prop.SetMethod != null
                && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
            )
            {
                methodsSource.AppendLine(
                    $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}set_{propName}\")]"
                );
                methodsSource.AppendLine(
                    $"        public static void set_{propName}(int {SelfPointerName}, {propType} value)"
                );
                methodsSource.AppendLine("        {");
                methodsSource.AppendLine(
                    $"            if (_instances.TryGetValue({SelfPointerName}, out var {Obj}))"
                );
                methodsSource.AppendLine("            {");
                methodsSource.AppendLine($"                {Obj}.{propName} = value;");
                methodsSource.AppendLine("            }");
                methodsSource.AppendLine("        }");
                methodsSource.AppendLine();
            }
        }

        var jsonMeta =
            @$"
#pragma warning disable CS0414 // field is assigned but its value is never used
        private static readonly string {ClassMetadata} =  
        """"""
        { JsonSerializer.Serialize(
            classMetadataBuilder.ClassInfo,
            DotWrapSerializerOptions.Default
        )}
        """""";
#pragma warning restore CS0414 // field is assigned but its value is never used";
        methodsSource.Append(jsonMeta);

        return methodsSource;
    }
}

public record ClassBuilderContext(INamedTypeSymbol ClassSymbol)
{
    public string ClassName => ClassSymbol.Name;
    public string Namespace => ClassSymbol.ContainingNamespace.ToDisplayString();
    public string WrapperName => ClassName + "Wrapper";
    public string FullyQualifiedWrapperName => $"{Namespace}.{WrapperName}";
    public string EntryPrefix => $"{Namespace.Replace(".", "_")}_{ClassName}_";
}
