# DotWrap

DotWrap is a package that automatically generates Python packages by wrapping AOT-compiled .NET libraries.

## Basic Usage

Suppose you have a c# class you want to call from Python:

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

That is all! A complete python package will be created in a new directory called `python-package-root`. Use the following commands to test it locally

```bash
cd ./python-package-root
pip install .
```

From there you should be able to import your project and any exposed types.

```python
import cool-calc

calc = cool-calc.Calculator()
result = calc.Add(2, 3)
print(result)  # Output: 5
```

---

This project is licensed under the MIT License. See the `LICENSE` file for details.
