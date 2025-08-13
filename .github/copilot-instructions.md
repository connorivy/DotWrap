# DotWrap - GitHub Copilot Instructions

## Project Overview

DotWrap is a .NET package that automatically generates Python packages by wrapping AOT-compiled .NET libraries, enabling c# library authors to distribute their libraries to Python developers with minimal effort. It leverages .NET Source Generators and MSBuild integration to create efficient, low-overhead bindings using CFFI and unmanaged interop.

## Architecture

### Core Components

1. **DotWrap.Shared** - Core attributes and shared functionality that is used by both the source generator and MSBuild tasks
2. **DotWrap.Generator** - Roslyn source generator that creates wrapper code at compile time
3. **DotWrap.MSBuild** - MSBuild integration that generates Python packages after build/publish
4. **DotWrap** - Operations used by the generated code at runtime as well as package metadata

### Key Technologies

-   .NET Source Generators (Roslyn)
-   MSBuild integration
-   UnmanagedCallersOnly interop
-   CFFI for Python bindings
-   AOT compilation for code portability

## Coding Patterns & Conventions

### Attributes System

-   Use `[DotWrapExpose]` to mark classes for Python exposure
-   Use `[DotWrapIgnore]` to exclude specific methods
-   Support external type exposure via assembly-level attributes
-   Follow the pattern: `DotWrapXXXAttribute` for all custom attributes

### Source Generation Patterns

```csharp
// Generated wrapper classes follow this pattern:
[UnmanagedCallersOnly(EntryPoint = "Namespace_Class_Method_Fingerprint")]
public static ReturnType Method_Fingerprint(IntPtr selfPtr, params...)
{
    var obj = __dotwrapGet(selfPtr);
    var result = obj.Method(params);
    return result;
}
```

### Memory Management

-   Use `GCHandle` for managed object lifetime management
-   Generate `__dotwrapCreate`, `__dotwrapGet`, `__dotwrapDestroy` methods
-   Always free handles in destructors

### Method Fingerprinting

-   Generate unique fingerprints for method overloads to avoid C export conflicts
-   Pattern: `MethodName_FINGERPRINT` where fingerprint is hash-based

## Development Guidelines

### When Working on Source Generators

-   Target `netstandard2.0` for compatibility
-   Use incremental generators (`IIncrementalGenerator`)
-   Include proper error handling and diagnostics
-   Test with both explicit and inferred type exposure

### When Working on MSBuild Integration

-   Support multi-targeting (.NET 8, 9, 10+)
-   Use `dotnet --roll-forward Major` for tool execution
-   Generate both build-time and publish-time artifacts
-   Create Python package structure in `python_project_root`

### File Generation Patterns

```
Generated Files:
- {ProjectName}.h - C header definitions
- main.py - Python wrapper classes
- lib_build.py - CFFI compilation script
- setup.py - Python package setup
```

### Testing Patterns

-   Use `[DotWrapExpose]` in test libraries
-   Test with various data types (primitives, strings, collections)
-   Include performance benchmarks comparing Python vs C#
-   Verify memory management with long-running tests

## Common Issues & Solutions

### Type System

-   Handle nullable reference types properly
-   Support generic types through type inference
-   Map C# types to appropriate Python types
-   Handle enums as separate wrapper classes

### Memory Safety

-   Always generate proper cleanup methods
-   Use RAII patterns in Python wrappers
-   Handle exceptions across the interop boundary
-   Validate pointer handles before use

### Build Integration

-   Ensure MSBuild targets run at correct lifecycle phases
-   Handle multi-framework targeting correctly
-   Copy native libraries to Python package during publish
-   Generate platform-specific builds

## Testing Strategy

-   Unit tests for source generators using Roslyn test infrastructure
-   Integration tests with real C# classes and Python consumption
-   Performance benchmarks to validate speed improvements
-   Cross-platform testing (Windows, Linux, macOS)

## Documentation Standards

-   Include XML documentation comments for all public APIs
-   Generate Python docstrings from C# XML comments
-   Maintain examples in README showing real usage
-   Document performance characteristics and limitations

## When Adding New Features

1. Consider both compile-time (source generator) and runtime (MSBuild) implications
2. Ensure compatibility with existing attribute system
3. Add appropriate test coverage in both C# and Python
4. Update documentation and examples
5. Consider cross-platform compatibility

This project bridges two language ecosystems, so always consider the developer experience from both C# and Python perspectives.
