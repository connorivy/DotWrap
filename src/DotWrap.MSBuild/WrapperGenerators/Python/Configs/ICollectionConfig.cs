using DotWrap.Configuration.Python;
using DotWrap.Extensions;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Internal.Constants;
using static DotWrap.Utils.Python.PythonConstants;

public class ICollectionConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(ICollection<>);

    public override void ConfigureGenericClassBody(PythonTypeConfigContext context)
    {
        var matchingType = context.MatchingType;
        var genericClassBodyBuilder = context.ClassBody;
        var typeInfo = context.ExportedType;

        var interfaceImplementation =
            matchingType.GetInterface(this.TypeToConfigure.Name)
            ?? throw new InvalidOperationException(
                $"Type {matchingType.FullName} does not implement ICollection<> interface."
            );
        var assemblyName =
            interfaceImplementation.GenericTypeArguments[0].AssemblyQualifiedName
            ?? interfaceImplementation.GenericTypeArguments[0].Name;
        var simplifiedAssemblyName = AssemblyNameUtils.GetSimplifiedAssemblyName(assemblyName);

        var genericArg = PythonNamingUtils.MapTypeToPython(
            DotWrapUtils.NormalizeCsTypeName(simplifiedAssemblyName),
            typeInfo.GenericTypeArgumentsToParameters,
            true
        );
        genericClassBodyBuilder?.AppendLine(
            $@"
def to_list(self) -> list[""{genericArg}""]:
    pass
        "
        );
    }

    public override void ConfigureClassBody(PythonTypeConfigContext context)
    {
        var typeDefinitions = context.TypeDefinitions;
        var matchingType = context.MatchingType;
        var classBody = context.ClassBody;
        var typeInfo = context.ExportedType;

        var interfaceImplementation =
            matchingType.GetInterface(this.TypeToConfigure.Name)
            ?? throw new InvalidOperationException(
                $"Type {matchingType.FullName} does not implement ICollection<> interface."
            );
        var collectionType = interfaceImplementation.GenericTypeArguments[0];
        var assemblyName =
            collectionType.AssemblyQualifiedName
            ?? throw new InvalidOperationException(
                $"Collection type {collectionType.Name} does not have a full name."
            );

        var simplifiedAssemblyName = AssemblyNameUtils.GetSimplifiedAssemblyName(assemblyName);
        var genericArg = PythonNamingUtils.MapTypeToPython(
            DotWrapUtils.NormalizeCsTypeName(simplifiedAssemblyName),
            typeInfo.GenericTypeArgumentsToParameters,
            false
        );
        var exposedType = DotWrapUtils.GetExposedTypeFromCsType(
            genericArg,
            out bool isOriginalType
        );
        classBody.AppendLine($"def to_list(self) -> list[\"{genericArg}\"]:");
        using var indent1 = classBody.IndentUntilDispose();

        var numpyType = PythonNamingUtils.MapTypeToNumpy(genericArg);
        classBody.AppendLine(
            @$"
""""""
Converts the array data to a list of the specified dtype.
""""""
{ExceptionInfoArg} = {Ffi}.new(""ExceptionInfo *"")
_raise_exception({ExceptionInfoArg})
length = {Lib}.{typeInfo.EntryPrefix}{GetCount}(self.{Ptr}, {ExceptionInfoArg})
arr = np.empty(length, dtype={numpyType})

# get stable pointer to the array data
arr_ptr = _dotwrap_ffi.cast(""void*"", _dotwrap_ffi.from_buffer(arr))
{ExceptionInfoArg}2 = {Ffi}.new(""ExceptionInfo *"")
{Lib}.{typeInfo.EntryPrefix}{FillArr}(self.{Ptr}, arr_ptr, length, {ExceptionInfoArg}2)
_raise_exception({ExceptionInfoArg}2)
                "
        );

        if (isOriginalType)
        {
            classBody.AppendLine("return arr.tolist()");
        }
        else
        {
            var innerTypeId = collectionType.GetExportedTypeIdFromType();
            var internalTypeDefinition = typeDefinitions[innerTypeId.ToString()];
            var externalTypeAssignment = PythonInteropUtils.GetExternalResultAssignment(
                internalTypeDefinition,
                false // todo: this is hardcoded for now
            );
            classBody.AppendLine("final_list = []");
            using (var forBlock = classBody.AppendLineWithNewBlock("for i in range(length):"))
            {
                if (numpyType == "np.intp")
                {
                    classBody.AppendLine($"{InternalPyResult} = {Ffi}.cast('void *', arr[i])");
                }
                else
                {
                    classBody.AppendLine($"{InternalPyResult} = arr[i]");
                }
                if (externalTypeAssignment != null)
                {
                    classBody.AppendLine($"{externalTypeAssignment}");
                    classBody.AppendLine($"final_list.append({ExportedPyResult})");
                }
                else
                {
                    classBody.AppendLine($"final_list.append({InternalPyResult})");
                }
            }
            classBody.AppendLine("return final_list");
        }
    }
}

public class IReadOnlyCollectionConfig : ICollectionConfig
{
    public override Type TypeToConfigure => typeof(IReadOnlyCollection<>);

    public override bool ShouldConfigure(PythonTypeConfigContext context)
    {
        var icollection = context.MatchingType.GetInterface(nameof(ICollection<int>));
        // var interfaces = context.MatchingType.GetInterfaces();
        return icollection is null;
    }
}
