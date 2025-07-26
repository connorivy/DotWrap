using System;
using System.Diagnostics;

namespace DotWrap.Tests;

public class ScratchPad
{
    [Test]
    public void TestMethod()
    {
        var x = float.MaxValue;
        var y = float.MinValue;

        // print to 10 decimal places
        Console.WriteLine($"Max: {x:F10}");
        Console.WriteLine($"Max: {y:F10}");

        Console.WriteLine($"Sum of primes: {x}");
    }
}
