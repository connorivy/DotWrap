using System.Reflection.Metadata;
using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Method;

public class InstanceMethodBuilder(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
{
    public void GenerateAllMethods(ClassBuilderContext classContext)
    {
        // Public instance methods
        foreach (
            var method in classContext
                .ClassSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public
                    && !m.IsStatic
                    && m.MethodKind == MethodKind.Ordinary
                    && !m.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == nameof(DotWrapIgnoreAttribute))
                )
        )
        {
            var context = new MethodBuilderContext(method, classContext);
            var methodXml = method.GetDocumentationCommentXml();
            var exportedMethodInfo = context.GetExportedMethodInfo(methodXml);

            foreach (var param in method.Parameters)
            {
                exportedMethodInfo.Parameters.Add(
                    new ExportedParameterInfo
                    {
                        Name = param.Name,
                        Type = param.Type.ToDisplayString(),
                        Comment = XmlParser.ParseParamComment(methodXml, param.Name),
                    }
                );
            }
            classMetadataBuilder.AddMethod(exportedMethodInfo);

            var returnType = GetExposedReturnTypeFromOriginal(context);
            switch (method.ReturnType.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_Int32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt16:
                case SpecialType.System_UInt32:
                case SpecialType.System_UInt64:
                case SpecialType.System_Void:
                    GenerateSingleMethod(context);
                    break;
                case SpecialType.System_String:
                    GenerateSingleMethodThatReturnsString(context);
                    break;
                default:
                    GenerateSingleMethodThatReturnsReferenceType(context);
                    break;
            }
        }
    }

    public void GenerateSingleMethod(MethodBuilderContext methodContext)
    {
        var entryPrefix = methodContext.ClassContext.EntryPrefix;
        var methodName = methodContext.MethodName;
        var returnType = GetExposedReturnTypeFromOriginal(methodContext);
        var methodSignature = methodContext.GetExposedMethodSignatureString();
        var internalMethodCallArgs = methodContext.GetInternalMethodCallArgumentsString();
        var convertParamsToInternal =
            methodContext.ConvertExposedParametersToInternalParametersTypes();

        var returnCall = methodContext.MethodSymbol.ReturnsVoid ? string.Empty : "return ";
        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static {returnType} {methodName}({methodSignature})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var {Obj} = {Get}({SelfPointerName});");
        if (convertParamsToInternal is not null)
        {
            sb.AppendLine(convertParamsToInternal);
        }
        sb.AppendLine($"            {returnCall}{Obj}.{methodName}({internalMethodCallArgs});");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    protected string GenerateSingleMethodThatReturnsString(MethodBuilderContext context)
    {
        var entryPrefix = context.ClassContext.EntryPrefix;
        var methodName = context.MethodName;
        var parameters = GetExposedParametersString(context);
        var args = string.Join(", ", context.MethodSymbol.Parameters.Select(p => p.Name));

        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static IntPtr {methodName}({parameters})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var {Obj} = {Get}({SelfPointerName});");
        sb.AppendLine(
            $"            return global::DotWrap.BuiltIn.CString.Create({Obj}.{methodName}({args}));"
        );
        sb.AppendLine("        }");
        sb.AppendLine();

        return sb.ToString();
    }

    protected string GenerateSingleMethodThatReturnsReferenceType(MethodBuilderContext context)
    {
        var entryPrefix = context.ClassContext.EntryPrefix;
        var returnType = context.MethodSymbol.ReturnType;
        var methodName = context.MethodName;
        var parameters = GetExposedParametersString(context);
        var args = string.Join(", ", context.MethodSymbol.Parameters.Select(p => p.Name));

        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static int {methodName}({parameters})");
        sb.AppendLine("        {");
        sb.AppendLine($"            var {Obj} = {Get}({SelfPointerName});");
        sb.AppendLine($"            var result = {Obj}.{methodName}({args});");
        sb.AppendLine($"            return {GetWrapperName(returnType)}.{Create}(result);");
        sb.AppendLine("        }");
        sb.AppendLine();

        return sb.ToString();
    }

    protected string GetExposedParametersString(MethodBuilderContext methodBuilderContext)
    {
        var parameters = string.Join(
            ", ",
            methodBuilderContext.MethodSymbol.Parameters.Select(p =>
                $"{GetExposedReturnTypeFromOriginal(p.Type)} {p.Name}"
            )
        );
        return $"int {SelfPointerName}{(parameters.Length > 0 ? ", " : "")}{parameters}";
    }

    protected static string GetExposedReturnTypeFromOriginal(MethodBuilderContext methodContext)
    {
        return GetExposedReturnTypeFromOriginal(methodContext.MethodSymbol.ReturnType);
    }

    public static string GetExposedReturnTypeFromOriginal(ITypeSymbol returnType)
    {
        return returnType switch
        {
            // SpecialType.System_String => "global::DotWrap.System.CString",
            { SpecialType: SpecialType.System_String } => "IntPtr",
            { SpecialType: SpecialType.System_Boolean } => "bool",
            { SpecialType: SpecialType.System_Double } => "double",
            { SpecialType: SpecialType.System_Single } => "float",
            {
                SpecialType: SpecialType.System_Byte
                    or SpecialType.System_SByte
                    or SpecialType.System_UInt16
                    or SpecialType.System_UInt32
                    or SpecialType.System_UInt64
                    or SpecialType.System_Int16
                    or SpecialType.System_Int32
                    or SpecialType.System_Int64
            } => "int",
            { SpecialType: SpecialType.System_Void } => "void",
            _ => "int", // everything else gets mapped to an int id
        };
    }

    protected static string GetWrapperName(ITypeSymbol returnType)
    {
        if (returnType is not INamedTypeSymbol namedType)
        {
            throw new NotSupportedException($"Unsupported return type: {returnType}");
        }

        ClassBuilderContext context = new ClassBuilderContext(namedType);
        return context.FullyQualifiedWrapperName;
    }
}

public record MethodBuilderContext(IMethodSymbol MethodSymbol, ClassBuilderContext ClassContext)
{
    public string MethodName => MethodSymbol.Name;

    public ExportedMethodInfo GetExportedMethodInfo()
    {
        var xmlDoc = MethodSymbol.GetDocumentationCommentXml();
        return GetExportedMethodInfo(xmlDoc);
    }

    public ExportedMethodInfo GetExportedMethodInfo(string? xmlDoc)
    {
        return new ExportedMethodInfo
        {
            Name = MethodName,
            ReturnType = MethodSymbol.ReturnType.ToDisplayString(),
            SummaryComment = XmlParser.ParseSummary(xmlDoc),
            ReturnsComment = XmlParser.ParseReturns(xmlDoc),
        };
    }

    public List<ParameterDetails> GetParameterDetails()
    {
        return MethodSymbol
            .Parameters.Select(p => new ParameterDetails(
                p.Name,
                InstanceMethodBuilder.GetExposedReturnTypeFromOriginal(p.Type),
                p.Type as INamedTypeSymbol
                    ?? throw new NotSupportedException($"Unsupported parameter type: {p.Type}")
            ))
            .ToList();
    }

    public string GetExposedMethodSignatureString()
    {
        var parameters = string.Join(
            ", ",
            GetParameterDetails().Select(p => $"{p.ExposedType} {p.Name}")
        );
        return $"int {SelfPointerName}{(parameters.Length > 0 ? ", " : "")}{parameters}";
    }

    public string? ConvertExposedParametersToInternalParametersTypes()
    {
        var parameters = string.Join(
            ", ",
            GetParameterDetails().Select(p => $"{p.ExposedType} {p.Name}")
        );
        StringBuilder sb = new();
        bool hasConverted = false;
        foreach (var param in GetParameterDetails())
        {
            if (param.OriginalType is null)
            {
                continue;
            }
            hasConverted = true;
            var classContext = new ClassBuilderContext(param.OriginalType);
            sb.Append(
                $"            var {param.Name}Typed = {classContext.WrapperName}.{Get}({param.Name});"
            );
        }

        return hasConverted ? sb.ToString() : null;
    }

    public string GetInternalMethodCallArgumentsString()
    {
        return string.Join(
            ", ",
            GetParameterDetails()
                .Select(p => $"{(p.OriginalType is null ? p.Name : $"{p.Name}Typed")}")
        );
    }
};

public record ParameterDetails(string Name, string ExposedType, INamedTypeSymbol? OriginalType);
