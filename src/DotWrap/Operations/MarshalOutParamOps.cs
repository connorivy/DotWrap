namespace DotWrap.Operations;

public static class MarshalOutParamOps
{
    public static void Marshal(byte value, IntPtr result)
    {
        System.Runtime.InteropServices.Marshal.WriteByte(result, value);
    }

    public static void Marshal(sbyte value, IntPtr result)
    {
        byte byteValue;
        unchecked
        {
            byteValue = (byte)value;
        }
        System.Runtime.InteropServices.Marshal.WriteByte(result, byteValue);
    }

    public static void Marshal(short value, IntPtr result)
    {
        System.Runtime.InteropServices.Marshal.WriteInt16(result, value);
    }

    public static void Marshal(ushort value, IntPtr result)
    {
        short shortValue;
        unchecked
        {
            shortValue = (short)value;
        }
        System.Runtime.InteropServices.Marshal.WriteInt16(result, shortValue);
    }

    public static void Marshal(int value, IntPtr result)
    {
        System.Runtime.InteropServices.Marshal.WriteInt32(result, value);
    }

    public static void Marshal(uint value, IntPtr result)
    {
        int intValue;
        unchecked
        {
            intValue = (int)value;
        }
        System.Runtime.InteropServices.Marshal.WriteInt32(result, intValue);
    }

    public static void Marshal(long value, IntPtr result)
    {
        System.Runtime.InteropServices.Marshal.WriteInt64(result, value);
    }

    public static void Marshal(ulong value, IntPtr result)
    {
        long longValue;
        unchecked
        {
            longValue = (long)value;
        }
        System.Runtime.InteropServices.Marshal.WriteInt64(result, longValue);
    }

    public static void Marshal(float value, IntPtr result)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, result, bytes.Length);
    }

    public static void Marshal(double value, IntPtr result)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, result, bytes.Length);
    }

    public static void Marshal(IntPtr value, IntPtr result)
    {
        System.Runtime.InteropServices.Marshal.WriteIntPtr(result, value);
    }

    public static void Marshal(UIntPtr value, IntPtr result)
    {
        IntPtr intPtrValue;
        unchecked
        {
            intPtrValue = (IntPtr)value;
        }
        System.Runtime.InteropServices.Marshal.WriteIntPtr(result, intPtrValue);
    }

    public static void Marshal(string value, IntPtr result)
    {
        var ptr = StringOperations.Alloc(value);
        Marshal(ptr, result);
    }

    public static void Marshal(Guid value, IntPtr result)
    {
        var bytes = value.ToByteArray();
        System.Runtime.InteropServices.Marshal.Copy(bytes, 0, result, 16);
    }
}
