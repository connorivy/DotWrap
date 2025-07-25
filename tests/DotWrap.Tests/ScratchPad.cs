using System;
using System.Diagnostics;

namespace DotWrap.Tests;

public class ScratchPad
{
    [Test]
    public void TestMethod()
    {
        var x = IsPrimeBench.SumOfPrimes();
        Console.WriteLine($"Sum of primes: {x}");
    }
}
