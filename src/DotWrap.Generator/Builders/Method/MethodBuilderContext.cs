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

    public ITypeSymbol ReturnType =>
        MethodSymbol.MethodKind is MethodKind.Constructor
            ? ClassContext.ClassSymbol
            : MethodSymbol.ReturnType;

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
            parameters = parameters.Prepend($"int {SelfPointerName}");
        }

        return string.Join(", ", parameters);
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
