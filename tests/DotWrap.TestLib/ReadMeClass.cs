using DotWrap;

namespace CoolCalc;

/// <summary>
/// A simple calculator class.
/// </summary>
[DotWrapExpose] // <-- mark with attr for source generator discoverablity
public class Calculator
{
    /// <summary>
    /// Adds two integers together.
    /// </summary>
    /// <param name="a">
    /// The first integer to add.
    /// </param>
    /// <param name="b">
    /// The second integer to add.
    /// </param>
    /// <returns>The sum of the two integers.</returns>
    public int Add(int a, int b) => a + b;
}
