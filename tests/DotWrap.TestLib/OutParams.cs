namespace DotWrap.TestLib;

[DotWrapExpose]
public class OutParams
{
    public static void OutInt42(out int result)
    {
        result = 42;
    }

    public static void OutStringHelloWorld(out string result)
    {
        result = "HelloWorld";
    }

    public static void OutIntEnumOne(out MyIntEnum result)
    {
        result = MyIntEnum.One;
    }

    public static void OutByteEnumTwo(out MyByteEnum result)
    {
        result = MyByteEnum.Two;
    }

    // public static void OutCustomStruct(out MyStruct result)
    // {
    //     result = new MyStruct
    //     {
    //         X = 1,
    //         Y = 2
    //     };
    // }

    public static void OutCustomClass(out MyClass result)
    {
        result = new MyClass
        {
            X = 3,
            Y = 4
        };
    }
}

public class MyClass
{
    public int X { get; set; }
    public int Y { get; set; }
}

// public struct MyStruct
// {
//     public int X { get; set; }
//     public int Y { get; set; }
// }

public enum MyIntEnum
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10
}

public enum MyByteEnum : byte
{
    Zero = 0,
    One = 1,
    Two = 2,
    Three = 3,
    Four = 4,
    Five = 5,
    Six = 6,
    Seven = 7,
    Eight = 8,
    Nine = 9,
    Ten = 10
}