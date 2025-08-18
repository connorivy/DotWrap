namespace DotWrap.Configuration;

/// <summary>
/// This class cannot be instantiated. Rather it is a marker that can be used while
/// specifying specific method signatures to indicate that any type is acceptable.
///
/// It is useful when you are trying to configure a method on an open generic type
/// where the specific type is not known at compile time
/// </summary>
public sealed class AnyType
{
    internal AnyType() { }
}
