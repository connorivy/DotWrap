using System.Text;
using DotWrap.Generator.Builders.Method;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Class;

public class ExplicitWrapperBuilder(ClassBuilderContext context)
    : EntryPointStaticClassBuilderBase(context)
{
    public override void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    )
    {
        this.AddInstanceMethods(methodsSource, classMetadataBuilder);
    }

    protected void AddInstanceMethods(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
    {
        var className = Context.ClassName;
        var entryPrefix = Context.EntryPrefix;
        InstanceMethodBuilder instanceMethodBuilder = new(sb, classMetadataBuilder);
        instanceMethodBuilder.GenerateAllMethods(Context);

        var classSymbol = Context.ClassSymbol;
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
                sb.AppendLine(
                    $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}get_{propName}\")]"
                );
                sb.AppendLine(
                    $"        public static {propType} get_{propName}(int {SelfPointerName})"
                );
                sb.AppendLine("        {");
                sb.AppendLine(
                    $"            if (!_instances.TryGetValue({SelfPointerName}, out var {Obj}))"
                );
                sb.AppendLine(
                    $"                throw new System.ArgumentException(\"Invalid instance handle: {SelfPointerName}\");"
                );
                sb.AppendLine($"            return {Obj}.{propName};");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
            // Setter
            if (
                prop.SetMethod != null
                && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
            )
            {
                sb.AppendLine(
                    $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}set_{propName}\")]"
                );
                sb.AppendLine(
                    $"        public static void set_{propName}(int {SelfPointerName}, {propType} value)"
                );
                sb.AppendLine("        {");
                sb.AppendLine(
                    $"            if (_instances.TryGetValue({SelfPointerName}, out var {Obj}))"
                );
                sb.AppendLine("            {");
                sb.AppendLine($"                {Obj}.{propName} = value;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
            }
        }
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
