using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Builders;
using DotWrap.Utils;
using DotWrap.Utils.Python;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Extensions
{
    public static class ExportedParameterTypeExtensions
    {
        extension(ExportedParameterInfo paramInfo)
        {
            public string PythonizeTypeName(IDictionary<string, string>? genericParamsToArgsDict,
                Dictionary<string, ExportedTypeDefinition> typeDefinitions, bool useGenericParams = false)
            {
                if (paramInfo.SpecialCaseFlags.HasFlag(ParameterSpecialCaseFlags.Out))
                {
                    var typeDef = typeDefinitions[paramInfo.Type.DefinitionId.ToString()];
                    var normalizedTypeName = DotWrapUtils.NormalizeCsTypeName(typeDef.TypeNameNoGenerics);
                    return $"\"Out{char.ToUpper(normalizedTypeName[0]) + normalizedTypeName.Substring(1)}\"";
                }
                return PythonNamingUtils.MapTypeToPython(
                    paramInfo.OriginalTypeName,
                    genericParamsToArgsDict,
                    useGenericParams
                );
            }
        }
    }
}