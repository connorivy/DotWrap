using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Method;

public class MethodBuilder(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
{
    public void GenerateAllMethods(ClassBuilderContext classContext)
    {
        foreach (
            var method in classContext
                .ClassSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public
                    && (m.MethodKind is MethodKind.Ordinary or MethodKind.Constructor)
                    && !m.GetAttributes()
                        .Any(a => a.AttributeClass?.Name == nameof(DotWrapIgnoreAttribute))
                )
        )
        {
            var context = new MethodBuilderContext(method, classContext);
            var methodXml = method.GetDocumentationCommentXml();

            List<ExportedParameterInfo> parameters = new(method.Parameters.Length);
            foreach (var param in method.Parameters)
            {
                var exposedCType = param.Type.GetExposedType(out var isOriginalType);
                parameters.Add(
                    new ExportedParameterInfo
                    {
                        Name = param.Name,
                        OriginalType = isOriginalType ? exposedCType : param.Type.ToDisplayString(),
                        ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
                        Comment = XmlParser.ParseParamComment(methodXml, param.Name),
                    }
                );
            }
            var exportedMethodInfo = context.GetExportedMethodInfo(methodXml, parameters);
            classMetadataBuilder.AddMethod(exportedMethodInfo);

            string? resultToExportTypePrefix;
            string? resultToExportTypeSuffix;
            if (context.ReturnType.SpecialType.IsBlittable())
            {
                resultToExportTypePrefix = null;
                resultToExportTypeSuffix = null;
            }
            else if (
                context.ReturnType.Name == "Half"
                && context.ReturnType.ContainingNamespace?.ToString() == "System"
            )
            {
                resultToExportTypePrefix = "(float)";
                resultToExportTypeSuffix = null;
            }
            else if (context.ReturnType.SpecialType == SpecialType.System_String)
            {
                resultToExportTypePrefix = "global::DotWrap.BuiltIn.CString.Create(";
                resultToExportTypeSuffix = ")";
            }
            else if (context.ReturnType.SpecialType == SpecialType.System_Boolean)
            {
                resultToExportTypePrefix = null;
                resultToExportTypeSuffix = " ? 1 : 0";
            }
            else
            {
                resultToExportTypePrefix = $"{GetWrapperName(context.ReturnType)}.{Create}(";
                resultToExportTypeSuffix = ")";
            }
            GenerateSingleMethod(
                context,
                exportedMethodInfo,
                resultToExportTypePrefix,
                resultToExportTypeSuffix
            );
        }
    }

    public void GenerateSingleMethod(
        MethodBuilderContext methodContext,
        ExportedMethodInfo exportedMethodInfo,
        string? resultToExportTypePrefix,
        string? resultToExportTypeSuffix
    )
    {
        var entryPrefix = methodContext.ClassContext.EntryPrefix;
        var methodName = exportedMethodInfo.StampedName;
        var OriginalMethodName = methodContext.OriginalMethodName;
        var returnType = methodContext.ReturnType.GetExposedType(out _);
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
        if (methodContext.IsStatic)
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

        var returnCall =
            methodContext.ReturnType.SpecialType == SpecialType.System_Void
                ? string.Empty
                : "return ";

        var methodCall =
            methodContext.MethodSymbol.MethodKind is MethodKind.Constructor
                ? $"new {obj}"
                : $"{obj}.{OriginalMethodName}";

        sb.AppendLine(
            $"            {returnCall}{resultToExportTypePrefix}{methodCall}({internalMethodCallArgs}){resultToExportTypeSuffix};"
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
