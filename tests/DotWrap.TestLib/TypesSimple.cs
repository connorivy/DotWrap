namespace DotWrap.TestLib;

[DotWrapExpose]
public class TypesSimple
{
    public static sbyte SByte(sbyte value) => value;

    public static byte Byte(byte value) => value;

    public static short Int16(short value) => value;

    public static ushort UInt16(ushort value) => value;

    public static int Int32(int value) => value;

    public static uint UInt32(uint value) => value;

    public static long Int64(long value) => value;

    public static ulong UInt64(ulong value) => value;

    // Floating point types
    public static Half Half(Half value) => value;

    public static float Single(float value) => value;

    public static double Double(double value) => value;
    public static Guid Guid(Guid value) => value;

    // todo:
    // public static decimal Decimal() => decimal.Value;

    public static bool Bool(bool value) => value;

    // String type
    public static string String(string value) => value;

    public static char Char(char value) => value;
}
