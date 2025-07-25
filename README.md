# DotWrap

DotWrap is a package for .NET 5+ projects that automatically generates Python packages by wrapping AOT-compiled .NET DLLs.

## Basic Usage

Suppose you have a .NET class you want to call from Python:

```csharp
using DotWrap;

namespace CoolCalc;

[DotWrapExpose] // <-- mark with attr for source generator discoverablity
public class Calculator
{
    public int Add(int a, int b) => a + b;
}
```

Next publish you package with

```bash
dotnet publish -r linux-x64 (or win-x64)
```

A folder called `python-package-root` will automatically be created in your project directory. This is a complete pip package that is ready for push to PyPI. But first you will probably want to test your package.
cd into the `python-package-root` dir and run `pip install .`

From there you should be able to import your project and any exposed types.

```python
import cool-calc

calc = cool-calc.Calculator()
result = calc.Add(2, 3)
print(result)  # Output: 5
```

---

This project is licensed under the MIT License. See the `LICENSE` file for details.
