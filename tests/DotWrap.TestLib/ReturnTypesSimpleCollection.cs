using System.Runtime.InteropServices;
using System.Text.Json;
using DotWrap.MSBuild;

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

[StructLayout(LayoutKind.Sequential)]
public struct ArrayInfo
{
    public IntPtr Ptr;
    public int Length;
}

// [DotWrapGenerated]
[DotWrapExpose]
public static class Hello
{
    public static void CopyArrayInfoToNumpyArray(ArrayInfo arrayInfo, IntPtr numpyArrayPtr)
    {
        int length = arrayInfo.Length;
        IntPtr srcPtr = arrayInfo.Ptr;
        IntPtr dstPtr = numpyArrayPtr;

        unsafe
        {
            IntPtr* src = (IntPtr*)srcPtr;
            IntPtr* dst = (IntPtr*)dstPtr;

            for (int i = 0; i < length; i++)
            {
                dst[i] = src[i];
            }
        }
    }

    // private static string __dotwrapMetadata = JsonSerializer.Serialize(
    //     new ExportedClassInfo
    //     {
    //         Namespace = "DotWrap.TestLib",
    //         EntryPrefix = "DotWrap_TestLib_",
    //         ClassName = nameof(ReturnTypesSimpleCollection),
    //         Methods = new List<ExportedMethodInfo>
    //         {
    //             new ExportedMethodInfo
    //             {
    //                 OriginalName = nameof(ReturnTypesSimpleCollection.GetInt32Array),
    //                 ExposedTypeIfDifferent = null,
    //                 OriginalType = "void",
    //                 IsStatic = true,
    //                 Parameters = new List<ExportedParameterInfo>()
    //                 {
    //                     new ExportedParameterInfo
    //                     {
    //                         Name = "arrayInfo",
    //                         OriginalType = "ArrayInfo",
    //                         ExposedTypeIfDifferent = null,
    //                     },
    //                     new ExportedParameterInfo
    //                     {
    //                         Name = "numpyArrayPtr",
    //                         OriginalType = "IntPtr",
    //                         ExposedTypeIfDifferent = null,
    //                     },
    //                 },
    //             },
    //         },
    //     },
    //     DotWrapSerializerOptions.Default
    // );
}
