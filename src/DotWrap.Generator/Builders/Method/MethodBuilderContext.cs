using System.Text;
using DotWrap.Generator.Builders.Class;
using DotWrap.Generator.Extensions;
using DotWrap.MSBuild;
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

    public ITypeSymbol ReturnType
    {
        get
        {
            if (ClassContext.ClassSymbol.GetUnderlyingEnumType() is INamedTypeSymbol underlyingType)
            {
                return underlyingType;
            }

            return MethodSymbol.MethodKind is MethodKind.Constructor
                ? ClassContext.ClassSymbol
                : MethodSymbol.ReturnType;
        }
    }

    public bool IsIndexer =>
        MethodSymbol.AssociatedSymbol is IPropertySymbol propertySymbol && propertySymbol.IsIndexer;

    public ExportedMethodInfo GetExportedMethodInfo(
        string? xmlDoc,
        List<ExportedParameterInfo> parameters
    )
    {
        var exposedCType = this.ReturnType.GetExposedType(out var isOriginalType);
        return new ExportedMethodInfo
        {
            OriginalName = MethodName,
            OriginalType = isOriginalType ? exposedCType : this.ReturnType.ToDisplayString(),
            IsStatic = this.IsStatic,
            ExposedTypeIfDifferent = isOriginalType ? null : exposedCType,
            GenericTypeName = (
                this.MethodSymbol.OriginalDefinition.ReturnType as ITypeParameterSymbol
            )?.Name,
            SpecialCaseFlags = this.GetSpecialCaseFlags(),
            SummaryComment = XmlParser.ParseSummary(xmlDoc),
            ReturnsComment = XmlParser.ParseReturns(xmlDoc),
            Parameters = parameters,
        };
    }

    public List<ParameterDetails> GetParameterDetails()
    {
        return MethodSymbol
            .Parameters.Select(p => new ParameterDetails(
                p.Name,
                p.Type.GetExposedType(out var isOriginalType),
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
        var parameters = GetParameterDetails().Select(p => $"{p.ExposedType} {p.Name}");
        if (!this.IsStatic)
        {
            parameters = parameters.Prepend($"{SelfPtrType} {SelfPointerName}");
        }

        return string.Join(", ", parameters);
    }

    public string? ConvertExposedParametersToInternalParametersTypes()
    {
        StringBuilder sb = new();
        bool hasConverted = false;
        foreach (var param in GetParameterDetails())
        {
            if (param.OriginalTypeIfDifferent is null)
            {
                continue;
            }
            hasConverted = true;
            var paramTypeClassContext = new ClassBuilderContext(
                ClassContext.GlobalContext,
                param.OriginalTypeIfDifferent
            );
            sb.Append(
                $"            var {param.Name}{Typed} = {paramTypeClassContext.WrapperName}.{Get}({param.Name});"
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

        return flags;
    }
};
