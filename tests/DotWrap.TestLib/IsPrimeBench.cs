using System;

namespace DotWrap.TestLib;

/// <summary>
/// A simple benchmark that returns the sum of all prime numbers less than 500,000
/// </summary>
[DotWrapExpose]
public class IsPrimeBench
{
    /// <summary>
    /// Returns the sum of all prime numbers less than 500,000
    /// </summary>
    /// <returns>
    /// The sum of all prime numbers less than 500,000
    /// </returns>
    public static long SumOfPrimes()
    {
        long sum = 0;
        for (int i = 2; i < 500_000; i++)
        {
            if (IsPrime(i))
            {
                sum += i;
            }
        }
        return sum;
    }

    private static bool IsPrime(int number)
    {
        if (number <= 1)
            return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0)
                return false;
        }
        return true;
    }
}
