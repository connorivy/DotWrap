using DotWrap.Configuration;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders
{
    public class OutParamWrapperBuilder
    {
        public static void CreateOutParamWrapper(
            OutParamInfo outParamInfo,
            IndentedPythonStringBuilder mainPy,
            IndentedPythonStringBuilder initPy
        )
        {
            var typeName = outParamInfo.TypeName.Replace("\"", string.Empty);
            initPy.AppendLine($"from .main import {typeName}");

            var externalResultAssignment = PythonInteropUtils.GetExternalResultAssignment(
                outParamInfo.ExportedTypeDefinition
            );
            Logger.LogDebug(
                $"Creating out param wrapper for {typeName} {outParamInfo.ExportedTypeDefinition.TypeNameNoGenerics} with assignment: {externalResultAssignment}"
            );
            mainPy.AppendLine(
                @$"
class {typeName}:
    def __init__(self):
        self.{OutVal} = {Ffi}.new({GetFfiType(outParamInfo.ExportedTypeDefinition.ExportedType)})
        self._field = None

    @property
    def value(self) -> {PythonNamingUtils.MapTypeToPython(DotWrapUtils.NormalizeCsTypeName(AssemblyNameUtils.GetSimplifiedAssemblyName(outParamInfo.ExportedTypeDefinition.AssemblyQualifiedName)))}:
        if self._field is None:
            {InternalPyResult} = self.{OutVal}[0]
            {externalResultAssignment}
            self._field = {(externalResultAssignment != null ? ExportedPyResult : InternalPyResult)}
        return self._field

    @property
    def {OutVal}(self):
        return self.__value
        
    @{OutVal}.setter
    def {OutVal}(self, value):
        self._field = None
        self.__value = value"
            );
        }

        private static string GetFfiType(ExportedType exportedType)
        {
            return exportedType switch
            {
                ExportedType.SByte => "\"signed char *\"",
                ExportedType.Byte => "\"unsigned char *\"",
                ExportedType.Int16 => "\"short *\"",
                ExportedType.UInt16 => "\"unsigned short *\"",
                ExportedType.Int32 => "\"int *\"",
                ExportedType.UInt32 => "\"unsigned int *\"",
                ExportedType.Int64 => "\"long long *\"",
                ExportedType.UInt64 => "\"unsigned long long *\"",
                ExportedType.Float => "\"float *\"",
                ExportedType.Double => "\"double *\"",
                ExportedType.IntPtr => "\"intptr_t *\"",
                ExportedType.Void => throw new NotImplementedException(),
                ExportedType.Char => throw new NotImplementedException(),
                _ => throw new NotSupportedException($"Unsupported exported type: {exportedType}"),
            };
        }
    }
}
