# Type Conversion Consolidation

This consolidation addresses issue #2 by centralizing scattered type conversion logic into a unified system.

## What was consolidated

### Before: Scattered Logic
- **DotWrap.MSBuild/WrapperGenerators/Python/Builders/MethodBuilderContext.cs**: `ConvertPythonParamsToCParams()` - hardcoded parameter conversion logic
- **DotWrap.Shared/Utils/Python/PythonInteropUtils.cs**: `GetExternalResultAssignment()` - hardcoded return value conversion logic  
- **DotWrap.Generator/Builders/Method/MethodBuilderContext.cs**: `ConvertExposedParametersToInternalParametersTypes()` - hardcoded C# type conversion logic
- **DotWrap.MSBuild/WrapperGenerators/Python/Extensions/DotWrapObjectExtensions.cs**: Various type mapping methods

### After: Centralized System
- **DotWrap.Shared/TypeConversion/TypeConversionService.cs**: Main service coordinating all conversions
- **DotWrap.Shared/TypeConversion/Converters/**: Individual type converters (StringTypeConverter, BooleanTypeConverter, etc.)
- **DotWrap.Shared/TypeConversion/ConversionContext.cs**: Context and result types for different conversion scenarios

## Types Handled
- **Primitives**: string, bool, char, System.Half
- **Enums**: Automatic detection via TypeSpecialCaseFlags.Enum
- **Complex Objects**: Wrapped objects with pointer management
- **Nullable Types**: Consistent null checking across all scenarios
- **Out Parameters**: Special handling for out parameter scenarios

## Benefits
1. **Maintainability**: All type conversion logic in one place
2. **Consistency**: Same conversion rules applied everywhere
3. **Extensibility**: Easy to add new types via new ITypeConverter implementations
4. **Testability**: Centralized logic can be unit tested
5. **Reduced Duplication**: Eliminates scattered hardcoded conversion logic

## Future Work
- **DotWrap.Generator/Builders/Method/MethodBuilderContext.cs**: `ConvertExposedParametersToInternalParametersTypes()` still uses hardcoded logic. This requires more complex refactoring due to dependencies on Roslyn ITypeSymbol objects, which would need an adapter layer to work with the consolidated system.

## Backward Compatibility
All existing public APIs remain unchanged. The consolidation is internal refactoring that doesn't affect external consumers.