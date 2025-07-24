using System;

namespace DotWrap.SampleAotLib;

/// <summary>
/// Sample class for demonstrating the capabilities of DotWrap.
/// </summary>
[DotWrapExpose]
public class SampleClass
{
    /// <summary>
    /// Instance method with no arguments. Name pretty much sums it up.
    /// </summary>
    /// <returns>
    /// Returns the integer 42.
    /// </returns>
    public int InstanceReturn42() => 42;

    /// <summary>
    /// </summary>
    /// <returns>
    /// Returns the value of Pi.
    /// </returns>
    public double InstanceReturnPi() => Math.PI;

    /// <summary>
    /// Static method that returns the value of Pi.
    /// </summary>
    /// <returns>
    /// Returns the value of Pi.
    /// </returns>
    public static double StaticReturnPi() => Math.PI;

    /// <summary>
    /// Instance method that is very dumb
    /// </summary>
    /// <returns>
    /// Returns the string "HelloWorld".
    /// </returns>
    public string InstanceReturnHelloWorld() => "HelloWorld";

    /// <summary>
    /// Static method that is very dumb
    /// </summary>
    /// <returns>
    /// Returns the string "HelloWorld".
    /// </returns>
    public static string StaticReturnHelloWorld() => "HelloWorld";

    /// <summary>
    /// </summary>
    /// <param name="value">A random integer</param>
    /// <returns>the same int</returns>
    public int InstanceTakesInt(int value)
    {
        return value;
    }

    /// <summary>
    /// Instance property int value
    /// </summary>
    public int InstanceProperty { get; set; }
}
