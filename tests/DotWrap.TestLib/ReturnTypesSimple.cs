namespace DotWrap.TestLib;

[DotWrapExpose]
public class ReturnTypesSimple
{
    public static sbyte MaxSByte() => sbyte.MaxValue;

    public static sbyte MinSByte() => sbyte.MinValue;

    public static byte MaxByte() => byte.MaxValue;

    public static byte MinByte() => byte.MinValue;

    public static short MaxInt16() => short.MaxValue;

    public static short MinInt16() => short.MinValue;

    public static ushort MaxUInt16() => ushort.MaxValue;

    public static ushort MinUInt16() => ushort.MinValue;

    public static int MaxInt32() => int.MaxValue;

    public static int MinInt32() => int.MinValue;

    public static uint MaxUInt32() => uint.MaxValue;

    public static uint MinUInt32() => uint.MinValue;

    public static long MaxInt64() => long.MaxValue;

    public static long MinInt64() => long.MinValue;

    public static ulong MaxUInt64() => ulong.MaxValue;

    public static ulong MinUInt64() => ulong.MinValue;

    // Floating point types
    public static Half MaxHalf() => Half.MaxValue;

    public static Half MinHalf() => Half.MinValue;

    public static float MaxSingle() => float.MaxValue;

    public static float MinSingle() => float.MinValue;

    public static double MaxDouble() => double.MaxValue;

    public static double MinDouble() => double.MinValue;

    public static Guid Guid_0123456789abcdef0123456789abcdef() => Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");

    // todo:
    // public static decimal MaxDecimal() => decimal.MaxValue;

    // public static decimal MinDecimal() => decimal.MinValue;

    // Boolean type
    public static bool True() => true;

    public static bool False() => false;

    // String type
    public static string HelloWorld() => "HelloWorld";

    // todo:
    // public static char FirstChar() => 'A';

    // Void type
    public static void DoNothing() { }
}
