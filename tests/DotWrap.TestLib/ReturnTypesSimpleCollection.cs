namespace DotWrap.TestLib;

[DotWrapExpose]
public class ReturnTypesSimpleCollection
{
    // public static sbyte[] GetSByteArray() => new[] { sbyte.MaxValue, sbyte.MinValue };

    // public static byte[] GetByteArray() => new[] { byte.MaxValue, byte.MinValue };

    // public static short[] GetInt16Array() => new[] { short.MaxValue, short.MinValue };

    // public static ushort[] GetUInt16Array() => new[] { ushort.MaxValue, ushort.MinValue };

    public static int[] GetInt32Array() => new[] { int.MaxValue, int.MinValue };

    // public static uint[] GetUInt32Array() => new[] { uint.MaxValue, uint.MinValue };

    // public static long[] GetInt64Array() => new[] { long.MaxValue, long.MinValue };

    // public static ulong[] GetUInt64Array() => new[] { ulong.MaxValue, ulong.MinValue };

    // public static float[] GetSingleArray() => new[] { float.MaxValue, float.MinValue };

    // public static double[] GetDoubleArray() => new[] { double.MaxValue, double.MinValue };
}
