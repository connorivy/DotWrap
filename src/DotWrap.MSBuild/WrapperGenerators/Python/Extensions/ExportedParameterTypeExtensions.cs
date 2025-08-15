using DotWrap.Configuration;
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
                var typeDef = typeDefinitions[paramInfo.Type.DefinitionId.ToString()];
                if (paramInfo.SpecialCaseFlags.HasFlag(ParameterSpecialCaseFlags.Out))
                {
                    return $"\"Out{char.ToUpper(typeDef.TypeNameNoGenerics[0]) + typeDef.TypeNameNoGenerics.Substring(1)}\"";
                }
                return PythonNamingUtils.MapTypeToPython(
                    typeDef.SimplifiedAssemblyQualifiedName,
                    genericParamsToArgsDict,
                    useGenericParams
                );
            }
        }
    }
}