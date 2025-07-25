using System;

namespace DotWrap.Tests;

[DotWrapExpose]
public class ReturnTypes
{
    // Integer types
    // public static short MaxInt16() => short.MaxValue;

    // public static int MaxInt32() => int.MaxValue;

    public static long MaxInt64() => long.MaxValue;

    // public static short MinInt16() => short.MinValue;

    // public static int MinInt32() => int.MinValue;

    // public static long MinInt64() => long.MinValue;

    // public static ushort MaxUInt16() => ushort.MaxValue;

    // public static uint MaxUInt32() => uint.MaxValue;

    // public static ulong MaxUInt64() => ulong.MaxValue;

    // public static ushort MinUInt16() => ushort.MinValue;

    // public static uint MinUInt32() => uint.MinValue;

    // public static ulong MinUInt64() => ulong.MinValue;

    // // Floating point types
    // public static float MaxSingle() => float.MaxValue;

    // public static double MaxDouble() => double.MaxValue;

    // public static float MinSingle() => float.MinValue;

    // public static double MinDouble() => double.MinValue;

    // // Boolean type
    // public static bool True() => true;

    // public static bool False() => false;

    // // String type
    // public static string HelloWorld() => "HelloWorld";

    // public static char FirstChar() => 'A';

    // // Void type
    // public static void DoNothing() { }

    // // Pointer type
    // public static IntPtr GetNullPointer() => IntPtr.Zero;
}
