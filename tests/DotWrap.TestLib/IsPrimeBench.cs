using System;
using DotWrap;

namespace DotWrap.Tests;

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

    // private static bool IsPrime(int n)
    // {
    //     if (n < 2)
    //         return false;
    //     if (n == 2)
    //         return true;
    //     if (n % 2 == 0)
    //         return false;
    //     int sqrt = (int)Math.Sqrt(n);
    //     for (int i = 3; i <= sqrt; i += 2)
    //         if (n % i == 0)
    //             return false;
    //     return true;
    // }
}
