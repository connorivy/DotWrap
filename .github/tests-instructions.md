# DotWrap Testing Instructions

## Test Project Overview
The DotWrap test suite consists of multiple test projects designed to verify different aspects of the C# to Python wrapper generation:

### Test Projects Structure
```
tests/
├── DotWrap.Tests/                    # C# unit tests (TUnit framework)
├── DotWrap.TestLib/                  # End-to-end test library (generates Python package)  
├── DotWrap.TestLib.DependencyLib/    # External dependency simulation
└── DotWrap.PythonTests/              # Python integration tests (pytest)
```

## Testing Frameworks Used
- **C# Unit Tests**: TUnit framework with snapshot testing via Verify
- **Python Tests**: pytest with asyncio support
- **End-to-End**: Custom test library that gets compiled to native and wrapped for Python

## Creating Debug/Test Code

### Adding C# Test Cases
To create debug code or test new wrapper functionality:

1. **Add test classes to `DotWrap.TestLib/`** - Create C# classes marked with `[DotWrapExpose]`:
```csharp
using DotWrap;

[DotWrapExpose]
public class MyTestClass
{
    public int TestMethod(int value) => value * 2;
}
```

2. **Follow existing patterns** - Look at files like:
   - `TypesSimple.cs` - Basic type testing
   - `TypesSimpleCollections.cs` - Collection type testing  
   - `Async.cs` - Async/await patterns
   - `Enums.cs` - Enum handling
   - `Properties.cs` - Property getter/setter testing

3. **Add corresponding Python tests** in `DotWrap.PythonTests/` following pytest conventions:
```python
import testlib

def test_my_test_class():
    instance = testlib.MyTestClass()
    result = instance.test_method(5)
    assert result == 10
```

### Creating Unit Tests for Source Generator
Add snapshot tests in `DotWrap.Tests/` to verify generated code:
```csharp
[Test]
public async Task TestNewFeature()
{
    var source = @"
using DotWrap;

[DotWrapExpose]
public class TestClass
{
    // Your test code here
}";

    await SnapshotVerifier.Verify(
        source,
        static result => result.Results[0].GeneratedSources[1].SourceText.ToString()
    );
}
```

## Running Tests

### C# Unit Tests with Filtering
Use treenode filters to run specific tests:

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test -- --treenode-filter /*/*/EnumTests/*

# Run tests starting with specific name
dotnet test -- --treenode-filter /*/*/EndToEndTests/StartsValue*

# Run single test method
dotnet test -- --treenode-filter /*/*/AssemblyNameFromSymbolTests/GetAssemblyNameFromSymbol_ReturnsExpectedName

# Use wildcards for broader matching
dotnet test -- --treenode-filter /*/*/String*/Test*
```

**Treenode Filter Format**: `/<Assembly>/<Namespace>/<Class>/<Method>`
- Supports wildcards (`*`)
- Supports operators: `and`, `or`, `starts with`, `ends with`, `equals`
- Case-sensitive matching

### Python Integration Tests

#### Full Test Suite (Recommended)
```bash
# Builds C# project, publishes, installs Python package, and runs pytest
./scripts/python_tests.sh
```

This script automatically:
1. Publishes `DotWrap.TestLib` with AOT compilation
2. Installs the generated Python package from `python_project_root/`
3. Runs all pytest tests

#### Manual Python Testing
```bash
# Ensure virtual environment is active
source venv/bin/activate

# Build and publish test library manually
dotnet publish ./tests/DotWrap.TestLib/DotWrap.TestLib.csproj -r linux-x64

# Install Python package
pip install ./tests/DotWrap.TestLib/python_project_root/

# Run specific Python tests
pytest tests/DotWrap.PythonTests/test_enums.py
pytest tests/DotWrap.PythonTests/test_async.py::test_task_of_int_await
pytest -v  # Verbose output
```

## Test Project Details

### DotWrap.TestLib (End-to-End Test Library)
- **Purpose**: Comprehensive test library that exercises all DotWrap features
- **Configuration**: 
  - `PublishAot=true` - Enables native AOT compilation
  - `EmitCompilerGeneratedFiles=true` - Outputs generated source files
  - Uses project references for development builds
- **Key Files**:
  - C# classes with `[DotWrapExpose]` attributes
  - `DotWrapConfig.cs` - Assembly-level configuration
  - `DotWrapPythonGlobalConfig.cs` - Python-specific settings
  - `python_project_root/` - Generated Python package directory

### DotWrap.Tests (Unit Tests)
- **Framework**: TUnit with Verify for snapshot testing
- **Target**: .NET 10.0 single framework
- **Testing Approach**: Source generator output verification
- **Key Features**:
  - Snapshot testing of generated wrapper code
  - Assembly qualified name testing
  - String manipulation utilities testing

### DotWrap.PythonTests (Integration Tests)
- **Framework**: pytest with asyncio support
- **Test Categories**:
  - `test_async.py` - Task/ValueTask async patterns
  - `test_enums.py` - Enum value handling
  - `test_exceptions.py` - Exception propagation
  - `test_nullable.py` - Nullable type handling
  - `test_properties.py` - Property getter/setters
  - `test_types_simple.py` - Basic type conversions
  - `test_types_simple_collection.py` - Collection type handling
  - `test_external_dependencies.py` - External type integration
  - `bench_*.py` - Performance benchmarking

### DotWrap.TestLib.DependencyLib
- **Purpose**: Simulates external assembly dependencies
- **Framework**: .NET 7.0 (different from main TestLib)
- **Usage**: Tests external type configuration and wrapper generation

## Test Data Patterns

### Common Test Scenarios
1. **Basic Types**: int, string, bool, double, etc.
2. **Collections**: List<T>, Dictionary<K,V>, arrays
3. **Nullable Types**: Both value types (int?) and reference types (string?)
4. **Async Patterns**: Task<T>, ValueTask<T>, async/await
5. **Enums**: Various enum configurations and value mappings
6. **Properties**: Getters, setters, and mixed access
7. **External Types**: Types from other assemblies
8. **Complex Generics**: Nested generic types and constraints

### Test Naming Conventions
- **C# Tests**: PascalCase method names with descriptive suffixes
- **Python Tests**: snake_case functions starting with `test_`
- **Test Files**: Match the feature area being tested

## Development Workflow for Testing

1. **Add C# test class** to `DotWrap.TestLib/`
2. **Run Python test script** to generate and install package: `./scripts/python_tests.sh`
3. **Add Python tests** in `DotWrap.PythonTests/` to verify behavior
4. **Add unit tests** in `DotWrap.Tests/` for source generator verification
5. **Use specific test filters** during development to speed up iteration

## Debugging Tips

### Generated Code Inspection
- Check `tests/DotWrap.TestLib/obj/Generated/` for source generator output
- Examine `tests/DotWrap.TestLib/python_project_root/` for Python package structure
- Generated C# wrapper methods have fingerprinting to avoid collisions

### Test Failures
- **Python import errors**: Run `./scripts/python_tests.sh` to rebuild everything
- **C# compilation errors**: Check that all required using statements are present
- **Missing types in Python**: Verify `[DotWrapExpose]` attribute is applied correctly
- **Test isolation**: Each test should create its own instances to avoid state sharing

### Performance Testing
- Use `bench_*.py` files as templates for performance measurements
- Compare C# vs Python execution times to verify overhead claims
- Monitor memory usage patterns during long-running tests

## Configuration Notes

### Build Configuration Differences
- **Development** (`ContinuousIntegrationEnv=false`): Uses project references, `DotWrapGenerateAfterBuild=true`
- **CI** (`ContinuousIntegrationEnv=true`): Uses NuGet packages, generates on publish only
- **Test Library**: Always uses Release configuration for publishing to ensure AOT optimizations

### Python Package Generation
- Native libraries copied during publish phase
- CFFI compilation happens during `pip install`
- Package structure follows Python conventions with proper `__init__.py` files
- Type hints and docstrings generated from XML documentation comments