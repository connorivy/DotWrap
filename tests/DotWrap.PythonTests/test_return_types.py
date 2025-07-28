from DotWrap_TestLib import ReturnTypesSimple as test_lib


def test_maxSByte():
    x = test_lib.MaxSByte()
    assert x == 127, f"Expected 127, got {x}"


def test_minSByte():
    x = test_lib.MinSByte()
    assert x == -128, f"Expected -128, got {x}"


def test_maxByte():
    x = test_lib.MaxByte()
    assert x == 255, f"Expected 255, got {x}"


def test_minByte():
    x = test_lib.MinByte()
    assert x == 0, f"Expected 0, got {x}"


def test_maxInt16():
    x = test_lib.MaxInt16()
    assert x == 32767, f"Expected 32767, got {x}"


def test_minInt16():
    x = test_lib.MinInt16()
    assert x == -32768, f"Expected -32768, got {x}"


def test_maxUInt16():
    x = test_lib.MaxUInt16()
    assert x == 65535, f"Expected 65535, got {x}"


def test_minUInt16():
    x = test_lib.MinUInt16()
    assert x == 0, f"Expected 0, got {x}"


def test_maxInt32():
    x = test_lib.MaxInt32()
    assert x == 2147483647, f"Expected 2147483647, got {x}"


def test_minInt32():
    x = test_lib.MinInt32()
    assert x == -2147483648, f"Expected -2147483648, got {x}"


def test_maxUInt32():
    x = test_lib.MaxUInt32()
    assert x == 4294967295, f"Expected 4294967295, got {x}"


def test_minUInt32():
    x = test_lib.MinUInt32()
    assert x == 0, f"Expected 0, got {x}"


def test_maxInt64():
    x = test_lib.MaxInt64()
    assert x == 9223372036854775807, f"Expected 9223372036854775807, got {x}"


def test_minInt64():
    x = test_lib.MinInt64()
    assert x == -9223372036854775808, f"Expected -9223372036854775808, got {x}"


def test_maxUInt64():
    x = test_lib.MaxUInt64()
    assert x == 18446744073709551615, f"Expected 18446744073709551615, got {x}"


def test_minUInt64():
    x = test_lib.MinUInt64()
    assert x == 0, f"Expected 0, got {x}"


def test_maxHalf():
    x = test_lib.MaxHalf()
    assert x == 65504, f"Expected 65504, got {x}"


def test_minHalf():
    x = test_lib.MinHalf()
    assert x == -65504, f"Expected -65504, got {x}"


def test_maxSingle():
    x = test_lib.MaxSingle()
    result = abs(x - 3.40282346638528859811704183484516925440e38)
    assert result < 1e-6, f"Expected close to 3.4028235e38, got {x}"


def test_minSingle():
    x = test_lib.MinSingle()
    assert abs(x + 3.40282346638528859811704183484516925440e38) < 1e-6, (
        f"Expected close to -3.4028235e38, got {x}"
    )


def test_maxDouble():
    x = test_lib.MaxDouble()
    assert abs(x - 1.7976931348623157e308) < 1e-9, (
        f"Expected close to 1.7976931348623157e308, got {x}"
    )


def test_minDouble():
    x = test_lib.MinDouble()
    assert abs(x + 1.7976931348623157e308) < 1e-9, (
        f"Expected close to -1.7976931348623157e308, got {x}"
    )


# def test_true():
#     x = test_lib.True()
#     assert x == True, f"Expected True, got {x}"
# def test_false():
#     x = test_lib.False()
#     assert x == False, f"Expected False, got {x}"
def test_helloWorld():
    x = test_lib.HelloWorld()
    assert x == "HelloWorld", f"Expected 'HelloWorld', got {x}"


def test_doNothing():
    x = test_lib.DoNothing()
    assert x is None, f"Expected None, got {x}"
