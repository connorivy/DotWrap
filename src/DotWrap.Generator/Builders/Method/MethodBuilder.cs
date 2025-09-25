using System.Runtime.CompilerServices;
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
            var method in GetMethodsToGenerate(classContext)
        )
        {
            GenerateSingleMethod(classContext, method);
        }
        //     foreach (
        //         var method in classContext
        //             .ClassSymbol.GetMembers()
        //             .OfType<IMethodSymbol>()
        //             .Where(m =>
        //                 m.DeclaredAccessibility == Accessibility.Public
        //                 && (
        //                     m.MethodKind
        //                     is MethodKind.Ordinary
        //                         or MethodKind.Constructor
        //                         or MethodKind.PropertyGet
        //                         or MethodKind.PropertySet
        //                 )
        //                 && !m.GetAttributes()
        //                     .Any(a => a.AttributeClass?.Name == nameof(DotWrapIgnoreAttribute))
        //                 && !IsCompilerGeneratedMethod(m)
        //                 && !m.IsInitOnly
        //             )
        //     )
        //     {
        //         GenerateSingleMethod(classContext, method);
        //     }
    }

    private IEnumerable<IMethodSymbol> GetMethodsToGenerate(ClassBuilderContext classContext)
    {
        var generatedMethods = new HashSet<string>();
        var currentClassSymbol = classContext.ClassSymbol;
        while (currentClassSymbol is not null &&
               currentClassSymbol.SpecialType != SpecialType.System_Object)
        {
            foreach (var method in currentClassSymbol
                .GetMembers()
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
                    && !IsCompilerGeneratedMethod(m)
                    && !m.IsInitOnly
                ))
            {
                var methodKeyComponents = method.Parameters
                    .Select(p => p.Type.ToDisplayString());

                if (method.MethodKind is MethodKind.Constructor)
                {
                    methodKeyComponents = methodKeyComponents
                        .Concat(classContext.ClassSymbol.GetRequiredMembers().Select(p => p.Type.ToDisplayString()));
                }

                var methodKey = $"{method.Name}({string.Join(", ", methodKeyComponents)})";

                if (generatedMethods.Add(methodKey))
                {
                    // there are a couple reasons why we need to do this:
                    // 1. we will encounter overriden methods in derived classes, and then again in base classes.
                    //    we only want to generate one method in the wrapper, so we skip the duplicates
                    // 2. for constructors we are promoting required parameters to constructor parameters in the 
                    //    generated wrapper. Therefore if a class has a ctor () with required properties (a, b) and 
                    //    a ctor (a, b) we need to only generate one of them because they will both become ctor(a, b) 
                    //    in the wrapper.
                    yield return method;
                }
            }
            currentClassSymbol = currentClassSymbol.BaseType;
        }
    }

    private static bool IsCompilerGeneratedMethod(IMethodSymbol method)
    {
        // Check for methods with angle brackets in the name, which indicates compiler-generated methods
        // Examples: <Clone>$, <Main>$, etc.
        if (method.Name.Contains('<') && method.Name.Contains('>'))
        {
            return true;
        }

        // Check if the method has the CompilerGenerated attribute
        return method.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");
    }

    public void GenerateSingleMethod(ClassBuilderContext classContext, IMethodSymbol method)
    {
        if (method.Parameters.Select(p => p.Type).Concat<ITypeSymbol>([method.ReturnType]).Any(p => p.IsRefLikeType))
        {
            Logger.LogWarning(
                $"Skipping method '{method.Name}' because it has a 'ref like' parameters or return type, which is not supported."
            );
            return;
        }

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
        classContext.GlobalContext.AddDiscoveredType(context.OriginalReturnType);
        foreach (var param in context.MethodSymbol.Parameters)
        {
            classContext.GlobalContext.AddDiscoveredType(param.Type);
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
            var requiredPropertySetters = methodContext.GetRequiredPropertySettersString();
            methodCall = $"new {obj}({internalMethodCallArgs})\n{requiredPropertySetters}";
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
