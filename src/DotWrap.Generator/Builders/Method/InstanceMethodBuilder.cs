using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
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
                var exposedCType = param.Type.GetExposedCType(out var isOriginalType);
                exportedMethodInfo.Parameters.Add(
                    new ExportedParameterInfo
                    {
                        Name = param.Name,
                        OriginalType = isOriginalType ? exposedCType : param.Type.ToDisplayString(),
                        ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
                        Comment = XmlParser.ParseParamComment(methodXml, param.Name),
                    }
                );
            }
            classMetadataBuilder.AddMethod(exportedMethodInfo);

            switch (method.ReturnType)
            {
                case { SpecialType: var st } when st.IsBlittable():
                    GenerateSingleMethod(context, null, null);
                    break;
                case { SpecialType: SpecialType.System_String }:
                    GenerateSingleMethod(context, "global::DotWrap.BuiltIn.CString.Create(", ")");
                    break;
                case { SpecialType: SpecialType.System_Boolean }:
                    GenerateSingleMethod(context, "", " ? 1 : 0");
                    break;
                default:
                    GenerateSingleMethod(
                        context,
                        $"{GetWrapperName(method.ReturnType)}.{Create}(",
                        ")"
                    );

                    break;
            }
        }
    }

    public void GenerateSingleMethod(
        MethodBuilderContext methodContext,
        string? resultToExportTypePrefix,
        string? resultToExportTypeSuffix
    )
    {
        var entryPrefix = methodContext.ClassContext.EntryPrefix;
        var methodName = methodContext.MethodName;
        var returnType = methodContext.MethodSymbol.ReturnType.GetExposedCType(out _);
        var methodSignature = methodContext.GetExposedMethodSignatureString();
        var internalMethodCallArgs = methodContext.GetInternalMethodCallArgumentsString();
        var convertParamsToInternal =
            methodContext.ConvertExposedParametersToInternalParametersTypes();

        sb.AppendLine(
            $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        );
        sb.AppendLine($"        public static {returnType} {methodName}({methodSignature})");
        sb.AppendLine("        {");

        string obj;
        if (methodContext.MethodSymbol.IsStatic)
        {
            obj = methodContext.ClassContext.ClassName;
        }
        else
        {
            obj = Obj;
            sb.AppendLine($"            var {obj} = {Get}({SelfPointerName});");
        }

        if (convertParamsToInternal is not null)
        {
            sb.AppendLine(convertParamsToInternal);
        }

        var returnCall = methodContext.MethodSymbol.ReturnsVoid ? string.Empty : "return ";

        sb.AppendLine(
            $"            {returnCall}{resultToExportTypePrefix}{obj}.{methodName}({internalMethodCallArgs}){resultToExportTypeSuffix};"
        );

        sb.AppendLine("        }");
        sb.AppendLine();
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
        var exposedCType = MethodSymbol.ReturnType.GetExposedCType(out var isOriginalType);
        return new ExportedMethodInfo
        {
            Name = MethodName,
            OriginalType = isOriginalType
                ? exposedCType
                : MethodSymbol.ReturnType.ToDisplayString(),
            ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
            SummaryComment = XmlParser.ParseSummary(xmlDoc),
            ReturnsComment = XmlParser.ParseReturns(xmlDoc),
        };
    }

    public List<ParameterDetails> GetParameterDetails()
    {
        return MethodSymbol
            .Parameters.Select(p => new ParameterDetails(
                p.Name,
                p.Type.GetExposedCType(out var isOriginalType),
                isOriginalType
                    ? null
                    : (
                        p.Type as INamedTypeSymbol
                        ?? throw new NotSupportedException(
                            $"Unsupported parameter type: {p.Type} on method {MethodSymbol.Name} in class {ClassContext.ClassSymbol.Name}"
                        )
                    )
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
            if (param.OriginalTypeIfDifferent is null)
            {
                continue;
            }
            hasConverted = true;
            var classContext = new ClassBuilderContext(param.OriginalTypeIfDifferent);
            sb.Append(
                $"            var {param.Name}{Typed} = {classContext.WrapperName}.{Get}({param.Name});"
            );
        }

        return hasConverted ? sb.ToString() : null;
    }

    public string GetInternalMethodCallArgumentsString()
    {
        return string.Join(
            ", ",
            GetParameterDetails()
                .Select(p => $"{(p.OriginalTypeIfDifferent is null ? p.Name : $"{p.Name}{Typed}")}")
        );
    }
};

public record ParameterDetails(
    string Name,
    string ExposedType,
    INamedTypeSymbol? OriginalTypeIfDifferent
);
