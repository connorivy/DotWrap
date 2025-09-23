using System.Text;
using DotWrap.Configuration;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Method;

public class MethodBuilder(
    StringBuilder sb,
    ClassMetadataBuilder classMetadataBuilder,
    ClassBuilderContext classContext
)
{
    public void GenerateAllMethods(ClassBuilderContext classContext)
    {
        foreach (
            var method in classContext
                .ClassSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m =>
                    m.DeclaredAccessibility == Accessibility.Public
                    && (
                        m.MethodKind
                        is MethodKind.Ordinary
                            or MethodKind.Constructor
                            or MethodKind.PropertyGet
                            or MethodKind.PropertySet
                    )
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
        ExportedMethodInfo exportedMethodInfo = GenerateMetadata(method, context);

        string? exportedResultAssignment;
        if (context.OriginalReturnType.SpecialType.IsBlittable())
        {
            exportedResultAssignment = null;
        }
        else if (
            context.OriginalReturnType.GetBlittableExternalTypeAssignment() is string assignment
        )
        {
            exportedResultAssignment = assignment;
        }
        else
        {
            exportedResultAssignment =
                @$"
            var {ExportedResult} = {GetWrapperName(context.OriginalReturnType)}.{Create}({InternalResult});";
        }
        GenerateSingleMethod(context, exportedMethodInfo, exportedResultAssignment);
    }

    private ExportedMethodInfo GenerateMetadata(IMethodSymbol method, MethodBuilderContext context)
    {
        var methodXml = method.GetDocumentationCommentXml();
        this.AddInferedTypes(context);

        List<ExportedParameterInfo> parameters = new(method.Parameters.Length);
        foreach (var param in method.Parameters)
        {
            var exposedCType = param.GetExposedType(out var isOriginalType);
            var genericName = (param.OriginalDefinition.Type as ITypeParameterSymbol)?.Name;
            parameters.Add(
                new ExportedParameterInfo
                {
                    Name = param.Name,
                    Type = param.Type.GetExportedTypeInstance(genericName),
                    OriginalTypeName = isOriginalType ? exposedCType : param.Type.ToDisplayString(),
                    ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
                    GenericTypeName = (param.OriginalDefinition.Type as ITypeParameterSymbol)?.Name,
                    Comment = XmlParser.ParseParamComment(methodXml, param.Name),
                    SpecialCaseFlags = param.GetSpecialCaseFlags(),
                }
            );
        }
        var exportedMethodInfo = context.GetExportedMethodInfo(methodXml, parameters);
        classMetadataBuilder.AddMethod(exportedMethodInfo);
        return exportedMethodInfo;
    }

    private void AddInferedTypes(MethodBuilderContext context)
    {
        classContext.GlobalContext.AddInferedType(context.OriginalReturnType);
        foreach (var param in context.MethodSymbol.Parameters)
        {
            classContext.GlobalContext.AddInferedType(param.Type);
        }
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
        var returnType = methodContext.OriginalReturnType.GetExposedType(out _);
        var methodSignature = methodContext.GetExposedMethodSignatureString();
        var internalMethodCallArgs = methodContext.GetInternalMethodCallArgumentsString();
        var convertParamsToInternal =
            methodContext.ConvertExposedParametersToInternalParametersTypes();
        var assignOutParameters = methodContext.AssignOutParameters();

        sb.AppendLine(
            @$"
        [UnmanagedCallersOnly(EntryPoint = ""{entryPrefix}{methodName}"")]
        public static {returnType} {methodName}({methodSignature})
        {{
            try
            {{
        "
        );
        // sb.AppendLine(
        //     $"        [UnmanagedCallersOnly(EntryPoint = \"{entryPrefix}{methodName}\")]"
        // );
        // sb.AppendLine($"        public static {returnType} {methodName}({methodSignature})");
        // sb.AppendLine("        {");

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

        string methodCall;
        bool isIndexer =
            methodContext.MethodSymbol.AssociatedSymbol is IPropertySymbol propertySymbol
            && propertySymbol.IsIndexer;
        if (methodContext.MethodSymbol.MethodKind is MethodKind.Constructor)
        {
            methodCall = $"new {obj}({internalMethodCallArgs})";
        }
        else if (methodContext.MethodSymbol.MethodKind is MethodKind.PropertyGet)
        {
            if (isIndexer)
            {
                methodCall = $"{obj}[{internalMethodCallArgs}]";
            }
            else
            {
                methodCall = $"{obj}.{OriginalMethodName["get_".Length..]}";
            }
        }
        else if (methodContext.MethodSymbol.MethodKind is MethodKind.PropertySet)
        {
            if (isIndexer)
            {
                methodCall = $"{obj}[index] = {internalMethodCallArgs.TrimStart("index, ").ToString()}";
            }
            else
            {
                methodCall =
                    $"{obj}.{OriginalMethodName["set_".Length..]} = {internalMethodCallArgs}";
            }
        }
        else if (methodContext.MethodSymbol.MethodKind is MethodKind.Conversion)
        {
            methodCall = $"({classContext.ClassSymbol.ToDisplayString()}){internalMethodCallArgs}";
        }
        else
        {
            methodCall = $"{obj}.{OriginalMethodName}({internalMethodCallArgs})";
        }

        string? internalResultAssignment;
        if (methodContext.OriginalReturnType.SpecialType == SpecialType.System_Void)
        {
            internalResultAssignment = null;
        }
        else
        {
            internalResultAssignment = $"var {InternalResult} = ";
        }

        sb.AppendLine($"            {internalResultAssignment}{methodCall};");

        if (assignOutParameters is not null)
        {
            sb.AppendLine(assignOutParameters);
        }

        if (exportedResultAssignment is not null)
        {
            sb.AppendLine(exportedResultAssignment);
            sb.AppendLine($"            return {ExportedResult};");
        }
        else if (internalResultAssignment is not null)
        {
            sb.AppendLine($"            return {InternalResult};");
        }

        sb.AppendLine(
            @$"
            }}
            catch (Exception e)
            {{
                DotWrap.Operations.ExceptionOps.HandleException(e, {ExceptionInfoPtr});
                {(internalResultAssignment is null ? "" : "return default!")};
            }}
        }}
        "
        );

        // sb.AppendLine("        }");
        // sb.AppendLine();
    }

    protected string GetWrapperName(ITypeSymbol returnType)
    {
        ClassBuilderContext newContext = new(classContext.GlobalContext, returnType, new());
        return newContext.FullyQualifiedWrapperName;
    }
}
