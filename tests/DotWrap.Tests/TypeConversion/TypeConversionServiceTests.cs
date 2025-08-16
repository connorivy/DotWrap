using System.Threading.Tasks;
using DotWrap.Configuration;
using DotWrap.TypeConversion;

namespace DotWrap.Tests.TypeConversion;

public class TypeConversionServiceTests
{
    private readonly ITypeConversionService _service = new TypeConversionService();

    [Test]
    public Task ConvertParameterPythonToC_String_ShouldGenerateExpression()
    {
        // Arrange
        var typeDefinition = CreateTestTypeDefinition("string", TypeSpecialCaseFlags.None);
        
        // Act & Assert - should not throw
        var result = _service.ConvertParameterPythonToC("testParam", typeDefinition, false);
        
        // Verify it's not null or empty
        if (string.IsNullOrEmpty(result))
            throw new System.Exception("Result should not be null or empty");
            
        return Task.CompletedTask;
    }

    [Test]
    public Task ConvertParameterPythonToC_Bool_ShouldGenerateExpression()
    {
        // Arrange
        var typeDefinition = CreateTestTypeDefinition("bool", TypeSpecialCaseFlags.None);
        
        // Act & Assert - should not throw
        var result = _service.ConvertParameterPythonToC("testParam", typeDefinition, false);
        
        if (string.IsNullOrEmpty(result))
            throw new System.Exception("Result should not be null or empty");
            
        return Task.CompletedTask;
    }

    [Test]
    public Task ConvertReturnValueCToPython_String_ShouldGenerateExpression()
    {
        // Arrange
        var typeDefinition = CreateTestTypeDefinition("string", TypeSpecialCaseFlags.None);
        
        // Act & Assert - should not throw
        var result = _service.ConvertReturnValueCToPython(typeDefinition, false);
        
        if (string.IsNullOrEmpty(result))
            throw new System.Exception("Result should not be null or empty");
            
        return Task.CompletedTask;
    }

    [Test]
    public Task ConvertReturnValueCToPython_Enum_ShouldGenerateExpression()
    {
        // Arrange
        var typeDefinition = CreateTestTypeDefinition("TestEnum", TypeSpecialCaseFlags.Enum);
        
        // Act & Assert - should not throw
        var result = _service.ConvertReturnValueCToPython(typeDefinition, false);
        
        if (string.IsNullOrEmpty(result))
            throw new System.Exception("Result should not be null or empty");
            
        return Task.CompletedTask;
    }

    private static ExportedTypeDefinition CreateTestTypeDefinition(string typeName, TypeSpecialCaseFlags flags)
    {
        return new ExportedTypeDefinition
        {
            Id = new ExportedTypeId(typeName),
            AssemblyQualifiedName = typeName,
            FullyQualifiedName = typeName,
            Namespace = string.Empty,
            SimplifiedAssemblyQualifiedName = typeName,
            EntryPrefix = string.Empty,
            GenericTypeArgumentsToParameters = new Dictionary<string, string>(),
            TypeNameNoGenerics = typeName,
            ExportedType = ExportedType.Undefined,
            SpecialCaseFlags = flags,
            IsSameAsExposedType = true,
            OriginalTypeWrapperName = string.Empty
        };
    }
}