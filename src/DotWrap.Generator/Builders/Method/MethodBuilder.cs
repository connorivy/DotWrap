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
            GenerateSingleMethod(classContext, method);
        }
    }

    public void GenerateSingleMethod(ClassBuilderContext classContext, IMethodSymbol method)
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

        string? exportedResultAssignment;
        if (context.ReturnType.SpecialType.IsBlittable())
        {
            exportedResultAssignment = null;
        }
        else if (
            context.ReturnType.Name == "Half"
            && context.ReturnType.ContainingNamespace?.ToString() == "System"
        )
        {
            exportedResultAssignment =
                @$"
            var {ExportedResult} = (float){InternalResult};";
        }
        else if (context.ReturnType.SpecialType == SpecialType.System_String)
        {
            exportedResultAssignment =
                @$"
            var {ExportedResult} = global::DotWrap.BuiltIn.CString.Create({InternalResult});";
        }
        else if (context.ReturnType.SpecialType == SpecialType.System_Boolean)
        {
            exportedResultAssignment =
                @$"
            var {ExportedResult} = {InternalResult} ? 1 : 0;";
        }
        else if (context.ReturnType is IArrayTypeSymbol arrayTypeSymbol)
        {
            exportedResultAssignment =
                @$"
            var {InternalPrefix}Arr = System.Runtime.InteropServices.Marshal.AllocHGlobal(sizeof({arrayTypeSymbol.ElementType.ToDisplayString()}) * {InternalResult}.Length);
            System.Runtime.InteropServices.Marshal.Copy({InternalResult}, 0, {InternalPrefix}Arr, {InternalResult}.Length);
            var {ExportedResult} = new ArrayInfo
            {{
                Ptr = {InternalPrefix}Arr,
                Length = {InternalResult}.Length
            }};
        ";
        }
        else
        {
            exportedResultAssignment =
                @$"
            var {ExportedResult} = {GetWrapperName(context.ReturnType)}.{Create}({InternalResult});";
        }
        GenerateSingleMethod(context, exportedMethodInfo, exportedResultAssignment);
    }

    private void GenerateSingleMethod(
        MethodBuilderContext methodContext,
        ExportedMethodInfo exportedMethodInfo,
        string? exportedResultAssignment
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

        if (methodContext.ReturnType.SpecialType == SpecialType.System_Void)
        {
            sb.AppendLine($"            {obj}.{OriginalMethodName}({internalMethodCallArgs});");
            sb.AppendLine("        }");
            sb.AppendLine();
            return;
        }

        var methodCall =
            methodContext.MethodSymbol.MethodKind is MethodKind.Constructor
                ? $"new {obj}"
                : $"{obj}.{OriginalMethodName}";

        sb.AppendLine(
            $"            var {InternalResult} = {methodCall}({internalMethodCallArgs});"
        );

        if (exportedResultAssignment is not null)
        {
            sb.AppendLine(exportedResultAssignment);
            sb.AppendLine($"            return {ExportedResult};");
        }
        else
        {
            sb.AppendLine($"            return {InternalResult};");
        }

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
