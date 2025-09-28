# DotWrap Development Instructions

## Project Overview
DotWrap is a tool that automatically generates **Python wrappers for .NET AOT projects**. It allows developers to write C# code once and generate native Python packages with full type hints, docstrings, and IntelliSense support. The generated packages have microsecond-level overhead (~0.3µs per call) and require no .NET runtime.

## Tech Stack
- **.NET**: Multi-targeting net8.0, net9.0, net10.0 with LangVersion=preview
- **C#**: Source generators, Roslyn analyzers, MSBuild tasks
- **Python**: CPython extension modules via CFFI, pytest for testing
- **Build System**: MSBuild with custom targets, cross-platform support
- **Package Management**: Central package management via Directory.Packages.props

## Architecture Overview
The project consists of 4 main components:
1. **DotWrap**: Meta-package containing attributes for marking types
2. **DotWrap.Generator**: Source generator creating wrapper code at compile time
3. **DotWrap.MSBuild**: MSBuild task for Python code generation
4. **DotWrap.Shared**: Shared types and utilities

## Project Structure
```
src/
├── DotWrap/                  # Main package with attributes
├── DotWrap.Generator/        # Source generator (netstandard2.0)
├── DotWrap.MSBuild/          # MSBuild tasks and Python generators
└── DotWrap.Shared/           # Shared configuration and utilities
tests/
├── DotWrap.TestLib/          # Test library for end-to-end testing
├── DotWrap.TestLib.DependencyLib/ # External dependency test
└── DotWrap.Tests/            # Unit tests
```

## Development Workflow

### Setup Environment
```bash
# Initial setup (done automatically in dev container)
source venv/bin/activate
pip install -r requirements.txt
dotnet restore
```

### Building and Testing
```bash
# Build the entire solution
dotnet build

# Run C# tests
dotnet test

# Build test library and run Python tests
dotnet publish ./tests/DotWrap.TestLib/DotWrap.TestLib.csproj -r linux-x64
pip install ./tests/DotWrap.TestLib/python_project_root/
pytest
```

**Use the provided scripts for testing:**
- `./scripts/python_tests.sh` - Builds test library, installs Python package, runs pytest

### Key Configuration Files
- **Directory.Build.props**: Global MSBuild properties (nullable enabled, preview C#)
- **Directory.Packages.props**: Central package version management
- **DotWrap.targets**: Custom MSBuild targets for wrapper generation
- **.editorconfig**: Code style (4 spaces, braces on new lines, avoid `this.`)

## Core Concepts

### Exposing Types to Python
Mark classes with `[DotWrapExpose]`:
```csharp
using DotWrap;

[DotWrapExpose]
public class Calculator
{
    public int Add(int a, int b) => a + b;
}
```

### External Type Configuration
Configure external types (like List<T>) using assembly-level attributes:
```csharp
[assembly: DotWrapExternalMethodMeta(typeof(List<>), "Add")]
[assembly: DotWrapExternalPropertyMeta(typeof(List<>), "Count")]
```

### Generation Process
1. **Compile**: Source generator creates `[UnmanagedCallersOnly]` wrapper methods
2. **Build**: MSBuild task generates Python classes and CFFI build scripts
3. **Publish**: Native libraries copied to Python package
4. **Install**: CFFI compiles CPython extension modules

## Coding Guidelines

### C# Style
- Use `var` sparingly - only when type is obvious
- Private fields: `camelCase`
- Expression-bodied members preferred for simple cases
- Place `using` directives outside namespace
- Enable nullable reference types (already configured)

### Python Integration
- C# → Python mappings defined in `wiki/Python.md/Mappings.md`
- List<T> → MutableSequence[T], Dictionary<K,V> → MutableMapping[K,V]
- Method names converted to snake_case (Add → append)

### Source Generation
- Generators target netstandard2.0 for Roslyn compatibility
- Use fingerprinting for method overloads to avoid name collisions
- Metadata stored in private fields (trimmed in AOT compilation)

## Common Tasks

### Adding New Wrapper Features
1. Update source generator in `DotWrap.Generator/UnmanagedCallersOnlyGenerator.cs`
2. Add Python code generation in `DotWrap.MSBuild/WrapperGenerators/Python/`
3. Update default configurations in `DefaultMeta.cs`
4. Add test cases in `DotWrap.TestLib/`

### Debugging Issues
- Source generator debug info enabled in development builds
- MSBuild task has debug print statements in `Directory.Build.targets`

### Local Development vs CI
- Development: `DotWrapGenerateAfterBuild=true` (regenerate on build)
- CI: Uses NuGet packages instead of project references
- Controlled by `ContinuousIntegrationEnv` property

## Testing Strategy

### End-to-End Testing
- `DotWrap.TestLib` project exposes various C# types
- Python tests in `tests/DotWrap.PythonTests/` verify functionality
- Benchmarks compare Python vs C# performance

### Unit Testing
- Snapshot testing with Verify framework
- Test generated code matches expected output
- Separate tests for different feature categories

## Build Considerations

### Cross-Platform
- Path separators handled via `DotWrapSlash` property
- Runtime identifiers: linux-x64, win-x64, osx-arm64, etc.

### Package Generation
- Multi-framework targeting (net8.0, net9.0, net10.0)
- MSBuild tools packaged per framework
- Source generators distributed as analyzers

## Performance Notes
- Wrapper calls have ~0.3µs overhead
- Memory management via GCHandle for C# objects
- AOT compilation eliminates .NET runtime dependency

## Known Limitations & TODOs
- Some "todo:" comments indicate areas for improvement
- Method selection could be more sophisticated than "first matching"
- Python package naming could be less opinionated

## Troubleshooting
- If build fails, ensure Python virtual environment is activated
- Check that the correct runtime identifier is used for publish commands
- Verify CFFI and other Python dependencies are installed via requirements.txt