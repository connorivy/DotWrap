# DotWrap Configuration Guide

## Configuring Types Exposed By CSharp

### Types Defined in Your Assembly

To expose a class from your own assembly to Python, use the `[DotWrapExpose]` attribute:

```csharp
using DotWrap;

[DotWrapExpose]
public class MyClass
{
    public int Add(int a, int b) => a + b;
}
```

#### Customizing Type and Namespace Names

You can customize the name and namespace as seen from Python using the `alias` and `namespaceAlias` parameters:

```csharp
[DotWrapExpose(alias: "Calculator", namespaceAlias: "math.utils")]
public class MyClass { ... }
```

This will generate a Python class `Calculator` in the `math.utils` namespace that will look like the following.

```python
import mylib

calc = mylib.math.utils.Calculator()
result = calc.Add(5, 5)
```

#### Excluding Methods

By default, all public methods and properties of exposed types will be included. To exclude a method or property from being exposed, use `[DotWrapIgnore]`:

```csharp
public class MyClass
{
    [DotWrapIgnore]
    public void JustForCSharp() { ... }
}
```

---

### Types Defined in Other Assemblies (External Types)

#### Exposing an External Type

As DotWrap is generating the external entry points for the types marked with [DotWrapExpose] (explicitly exposed types), it will keep track of all the additional types that are exposed by those methods and properties. These types are what I call implicitly exposed. Implicitly exposed types, unlike explicity exposed types, will generate a minimal type without any methods or properties on the python side.

```csharp
[DotWrapExpose]
public class MyClass
{
    public List<int> GetInts() => ...
    public void AcceptInts(List<int> ints) => ...
}
```

```python
class MyClass:
    def __init__(self):
        ...
    def GetInts(self) -> List[int]:
        ...
    def AcceptInts(self, List[int]):
        ...

TGeneric = TypeVar('TGeneric')
class List[Generic(TGeneric)]:

    # class only has internal state, no public methods or properties

    def __init__(self):
        ...
```

This enables the scenario where you library returns a type that can then be passed back into a different method in your library to do somethin.

#### Exposing External Methods

You can expose properties and methods in implicly exposed types, but you need to opt into the method with the correct attribute.

> [!NOTE]
> DotWrap configures some of the very common properties and methods of System class to be exposed by default
> These include List<>.Add, List<>[], ICollection.Count, Task.Status, and others
> You can see the default applied attributes in the UnmanagedCallersOnlyGenerator.cs

```csharp
[assembly: DotWrapExternalMethodMeta(typeof(List<>), "Add")]
[assembly: DotWrapExternalPropertyMeta(typeof(List<>), "Count"))]
[assembly: DotWrapExternalIndexerMeta(typeof(List<>))]
```

Now the generated python class will look something like this.

```python
TGeneric = TypeVar('TGeneric')
class List[Generic(TGeneric)]:
    def __init__(self):
        ...
    def Add(self, value: TGeneric) -> None:
        ...
    @property
    def Count(self) -> int:
        ...
    def __getItem__(self) -> int:
        ...
    def __setItem__(self) -> None:
        ...
```

#### Multiple Configurations

When creating an implicitly generated type, DotWrap will apply all Meta attributes where the type being configured is assignable to the type defined in the attribute. For example, if the type being generated is a List<int>, then all of the following attributes would be applied to that type

```csharp
[assembly: DotWrapExternalMethodMeta(typeof(List<int>), "Add")]
[assembly: DotWrapExternalMethodMeta(typeof(List<>), "Remove")]
[assembly: DotWrapExternalMethodMeta(typeof(IList<>), "Contains")]
[assembly: DotWrapExternalPropertyMeta(typeof(ICollection<>), "Count"))]
```

you can set the ignore: true if you want to ignore a method or property that you previous configured to be included. DotWrap does this by default with IList<>.Add and then later we configure Array to ignore this because arrays implement IList but are immutable

```csharp
[assembly: DotWrapExternalMethodMeta(typeof(Array), "Add", ignore: true)]
```

---

## Configuring Python

### Global Configuration

To configure the code that is create on the python side, you can create an instance of the 'DotWrapPythonGlobaConfig' abstract class in your assembly. This is a configuration that is used by default to generate the code that allows tasks to be awaitable

```csharp
internal class GlobalConfig : DotWrap.Configuration.Python.DotWrapPythonGlobalConfig
{
    public override string? PythonPackageName => "testlib";

    public override Dictionary<string, string>? NamespaceOverrides { get; } =
        new() { { "System.Threading.Tasks", "builtin.threading" } };
}
```

### Type Configuration

```csharp
internal class TaskConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(Task);

    public override void ConfigureGenericClassBody(PythonTypeConfigContext context)
    {
        var genericClassBodyBuilder = context.ClassBody;
        genericClassBodyBuilder?.AppendLine(
            @"
def __await__(self):
    return self._poll().__await__()

async def _poll(self):
    while True:
        status = self.status
        if status == TaskStatus.ran_to_completion:
            return self.result
        elif status == TaskStatus.faulted:
            raise RuntimeError(""Error polling task"")
        await asyncio.sleep(0.1)
        "
        );
    }
}
```

view the source for deeper description of the properties and how they affect the generated code

---

## Advanced Scenarios

-   **Namespace Aliasing:** Use `namespaceAlias` to avoid conflicts or to group types logically in Python.
-   **Selective Exposure:** Use `ignore` to exclude specific methods or properties from being exposed.
-   **Overload Resolution:** Use the `parameters` argument in `DotWrapExternalMethodMeta` to specify which overload to expose.
-   **Property Access Control:** Use `propertyType` in `DotWrapExternalPropertyMeta` and `DotWrapExternalIndexerMeta` to control getter/setter exposure.

---

## Best Practices

-   Use method aliases to avoid naming conflicts in Python.
-   Only expose the types and members you need for your Python API surface.
-   Use `[DotWrapIgnore]` and `ignore: true` to keep internal or unsafe methods out of the Python package.
-   Document your configuration with comments for maintainability.

---

For more details and examples, see the main [README](../README.md) and the [How It Works](HowItWorks.md) document.
