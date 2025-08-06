using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
using DotWrap.MSBuild;
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
            GetBlittableExternalTypeAssignment(
                context.OriginalReturnType,
                classContext.GlobalContext
            )
            is string assignment
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
            var exposedCType = param.Type.GetExposedType(out var isOriginalType);
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
                methodCall = $"{obj}[index] = value";
            }
            else
            {
                methodCall =
                    $"{obj}.{OriginalMethodName["set_".Length..]} = {internalMethodCallArgs}";
            }
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

        if (exportedResultAssignment is not null)
        {
            sb.AppendLine(exportedResultAssignment);
            sb.AppendLine($"            return {ExportedResult};");
        }
        else if (internalResultAssignment is not null)
        {
            sb.AppendLine($"            return {InternalResult};");
        }

        sb.AppendLine("        }");
        sb.AppendLine();
    }

    protected string GetWrapperName(ITypeSymbol returnType)
    {
        ClassBuilderContext newContext = new(classContext.GlobalContext, returnType);
        return newContext.FullyQualifiedWrapperName;
    }

    public static string? GetBlittableExternalTypeAssignment(
        ITypeSymbol typeSymbol,
        GlobalContext globalContext
    )
    {
        if (typeSymbol is null)
        {
            throw new ArgumentNullException(nameof(typeSymbol));
        }

        if (typeSymbol.Name == "Half" && typeSymbol.ContainingNamespace?.ToString() == "System")
        {
            return @$"
            var {ExportedResult} = (float){InternalResult};";
        }
        else if (typeSymbol.SpecialType == SpecialType.System_String)
        {
            return @$"
            var {ExportedResult} = global::DotWrap.BuiltIn.CString.Create({InternalResult});";
        }
        else if (typeSymbol.SpecialType == SpecialType.System_Boolean)
        {
            return @$"
            var {ExportedResult} = {InternalResult} ? 1 : 0;";
        }
        else if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var namedType =
                typeSymbol as INamedTypeSymbol
                ?? throw new ArgumentException(
                    "Expected typeSymbol to be a named type symbol for enum handling.",
                    nameof(typeSymbol)
                );
            var underlyingType =
                namedType.EnumUnderlyingType
                ?? throw new InvalidOperationException("Enum underlying type is null.");
            return @$"
            var {ExportedResult} = ({underlyingType.ToDisplayString()}){InternalResult};";
        }
        return null;
    }
}
