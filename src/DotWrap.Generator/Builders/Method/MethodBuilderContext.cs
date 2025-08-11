using System.Text;
using DotWrap.Configuration;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Method;

public record MethodBuilderContext(IMethodSymbol MethodSymbol, ClassBuilderContext ClassContext)
{
    public string MethodName =>
        Alias
        ?? (
            (MethodSymbol.MethodKind is MethodKind.Constructor) ? "Constructor" : MethodSymbol.Name
        );
    public string OriginalMethodName => MethodSymbol.Name;

    public AttributeData? Meta { get; } =
        MethodSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == nameof(DotWrapMetaAttribute));

    public string? Alias =>
        (
            this
                .Meta?.NamedArguments.Where(n => n.Key == "alias")
                .Select(n => n.Value.Value as string)
                .FirstOrDefault()
        ) ?? (this.Meta?.ConstructorArguments.FirstOrDefault().Value as string);

    public bool IsStatic =>
        MethodSymbol.IsStatic || MethodSymbol.MethodKind is MethodKind.Constructor;

    public ITypeSymbol OriginalReturnType =>
        MethodSymbol.MethodKind is MethodKind.Constructor
            ? ClassContext.ClassSymbol
            : MethodSymbol.ReturnType;

    public bool IsIndexer =>
        MethodSymbol.AssociatedSymbol is IPropertySymbol propertySymbol && propertySymbol.IsIndexer;

    public ExportedMethodInfo GetExportedMethodInfo(
        string? xmlDoc,
        List<ExportedParameterInfo> parameters
    )
    {
        var exposedCType = this.OriginalReturnType.GetExposedType(out var isOriginalType);
        var genericName = (
            this.MethodSymbol.OriginalDefinition.ReturnType as ITypeParameterSymbol
        )?.Name;
        return new ExportedMethodInfo
        {
            OriginalName = MethodName,
            OriginalTypeName = isOriginalType
                ? exposedCType
                : this.OriginalReturnType.ToDisplayString(),
            IsStatic = this.IsStatic,
            ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
            GenericTypeName = (
                this.MethodSymbol.OriginalDefinition.ReturnType as ITypeParameterSymbol
            )?.Name,
            ReturnType = this.OriginalReturnType.GetExportedTypeInstance(genericName),
            SpecialCaseFlags = this.GetSpecialCaseFlags(),
            SummaryComment = XmlParser.ParseSummary(xmlDoc),
            ReturnsComment = XmlParser.ParseReturns(xmlDoc),
            Parameters = parameters,
        };
    }

    public List<ParameterDetails> GetParameterDetails()
    {
        return MethodSymbol
            .Parameters.Select(p =>
            {
                var isOutParam = p.RefKind is RefKind.Out;
                var exposedCType = p.GetExposedType(out var isOriginalType);
                return new ParameterDetails(
                    p.Name,
                    exposedCType,
                    isOriginalType
                        ? null
                        : (
                            p.Type as INamedTypeSymbol
                            ?? throw new NotSupportedException(
                                $"Unsupported parameter type: {p.Type} on method {MethodSymbol.Name} in class {ClassContext.ClassSymbol.Name}"
                            )
                        ),
                    isOutParam
                );
            })
            .ToList();
    }

    public string GetExposedMethodSignatureString()
    {
        var parameters = GetParameterDetails().Select(p => $"{p.ExposedType} {p.Name}");
        if (!this.IsStatic)
        {
            parameters = parameters.Prepend($"{SelfPtrType} {SelfPointerName}");
        }
        parameters = parameters.Append($"IntPtr {ExceptionInfoPtr}");

        return string.Join(", ", parameters);
    }

    public string? ConvertExposedParametersToInternalParametersTypes()
    {
        StringBuilder sb = new();
        bool hasConverted = false;
        foreach (var param in GetParameterDetails())
        {
            if (param.OriginalTypeIfDifferent is null || param.IsOutParam)
            {
                continue;
            }
            hasConverted = true;

            if (param.OriginalTypeIfDifferent.TypeKind is TypeKind.Enum)
            {
                sb.AppendLine(
                    $"            var {param.Name}{Typed} = ({param.OriginalTypeIfDifferent.ToDisplayString()}){param.Name};"
                );
            }
            else
            {
                var paramTypeClassContext = new ClassBuilderContext(
                    ClassContext.GlobalContext,
                    param.OriginalTypeIfDifferent
                );
                sb.Append(
                    $"            var {param.Name}{Typed} = {paramTypeClassContext.WrapperName}.{Get}({param.Name});"
                );
            }
        }

        return hasConverted ? sb.ToString() : null;
    }

    public string GetInternalMethodCallArgumentsString()
    {
        return string.Join(
            ", ",
            GetParameterDetails()
                .Select(p =>
                {
                    string? paramPrefix = p switch
                    {
                        { IsOutParam: true } => $"out var ",
                        _ => null,
                    };
                    string? paramSufix = p switch
                    {
                        { IsOutParam: true } => $"{OutParam}",
                        { OriginalTypeIfDifferent: not null } => Typed,
                        _ => null,
                    };
                    return $"{paramPrefix}{p.Name}{paramSufix}";
                })
        );
    }

    public string? AssignOutParameters()
    {
        StringBuilder sb = new();
        bool hasConverted = false;
        foreach (var param in GetParameterDetails())
        {
            if (!param.IsOutParam)
            {
                continue;
            }
            hasConverted = true;

            sb.AppendLine(
                $"            DotWrap.Operations.MarshalOutParamOps.Marshal({param.Name}{OutParam}, {param.Name});"
            );
        }

        return hasConverted ? sb.ToString() : null;
    }

    public MethodSpecialCaseFlags GetSpecialCaseFlags()
    {
        MethodSpecialCaseFlags flags = MethodSpecialCaseFlags.None;

        if (this.MethodSymbol.IsStatic)
        {
            flags |= MethodSpecialCaseFlags.Static;
        }

        if (this.MethodSymbol.MethodKind is MethodKind.PropertyGet)
        {
            flags |= MethodSpecialCaseFlags.PropertyGetter;
        }
        if (this.MethodSymbol.MethodKind is MethodKind.PropertySet)
        {
            flags |= MethodSpecialCaseFlags.PropertySetter;
        }
        if (this.IsIndexer)
        {
            flags |= MethodSpecialCaseFlags.Indexer;
        }
        if (this.OriginalReturnType.TypeKind is TypeKind.Enum)
        {
            flags |= MethodSpecialCaseFlags.EnumReturnType;
        }

        return flags;
    }
};
