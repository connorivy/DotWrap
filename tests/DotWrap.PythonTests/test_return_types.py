import DotWrap_TestLib as test_lib


def test_maxSByte():
    x = test_lib.ReturnTypes.MaxSByte()
    assert x == 127, f"Expected 127, got {x}"
def test_minSByte():
    x = test_lib.ReturnTypes.MinSByte()
    assert x == -128, f"Expected -128, got {x}"
def test_maxByte():
    x = test_lib.ReturnTypes.MaxByte()
    assert x == 255, f"Expected 255, got {x}"
def test_minByte():
    x = test_lib.ReturnTypes.MinByte()
    assert x == 0, f"Expected 0, got {x}"
def test_maxInt16():
    x = test_lib.ReturnTypes.MaxInt16()
    assert x == 32767, f"Expected 32767, got {x}"
def test_minInt16():
    x = test_lib.ReturnTypes.MinInt16()
    assert x == -32768, f"Expected -32768, got {x}"
def test_maxUInt16():
    x = test_lib.ReturnTypes.MaxUInt16()
    assert x == 65535, f"Expected 65535, got {x}"
def test_minUInt16():
    x = test_lib.ReturnTypes.MinUInt16()
    assert x == 0, f"Expected 0, got {x}"
def test_maxInt32():
    x = test_lib.ReturnTypes.MaxInt32()
    assert x == 2147483647, f"Expected 2147483647, got {x}"
def test_minInt32():
    x = test_lib.ReturnTypes.MinInt32()
    assert x == -2147483648, f"Expected -2147483648, got {x}"
def test_maxUInt32():
    x = test_lib.ReturnTypes.MaxUInt32()
    assert x == 4294967295, f"Expected 4294967295, got {x}"
def test_minUInt32():
    x = test_lib.ReturnTypes.MinUInt32()
    assert x == 0, f"Expected 0, got {x}"
def test_maxInt64():
    x = test_lib.ReturnTypes.MaxInt64()
    assert x == 9223372036854775807, f"Expected 9223372036854775807, got {x}"
def test_minInt64():
    x = test_lib.ReturnTypes.MinInt64()
    assert x == -9223372036854775808, f"Expected -9223372036854775808, got {x}"
def test_maxUInt64():
    x = test_lib.ReturnTypes.MaxUInt64()
    assert x == 18446744073709551615, f"Expected 18446744073709551615, got {x}"
def test_minUInt64():
    x = test_lib.ReturnTypes.MinUInt64()
    assert x == 0, f"Expected 0, got {x}"
def test_maxHalf():
    x = test_lib.ReturnTypes.MaxHalf()
    assert x == 65504, f"Expected 65504, got {x}"
def test_minHalf():
    x = test_lib.ReturnTypes.MinHalf()
    assert x == -65504, f"Expected -65504, got {x}"
def test_maxSingle():
    x = test_lib.ReturnTypes.MaxSingle()
    assert x == 3.4028235e38, f"Expected 3.4028235e38, got {x}"
def test_minSingle():
    x = test_lib.ReturnTypes.MinSingle()
    assert x == -3.4028235e38, f"Expected -3.4028235e38, got {x}"
def test_maxDouble():
    x = test_lib.ReturnTypes.MaxDouble()
    assert x == 1.7976931348623157e308, f"Expected 1.7976931348623157e308, got {x}"
def test_minDouble():
    x = test_lib.ReturnTypes.MinDouble()
    assert x == -1.7976931348623157e308, f"Expected -1.7976931348623157e308, got {x}"
# def test_true():
#     x = test_lib.ReturnTypes.True()
#     assert x == True, f"Expected True, got {x}"
# def test_false():
#     x = test_lib.ReturnTypes.False()
#     assert x == False, f"Expected False, got {x}"
def test_helloWorld():
    x = test_lib.ReturnTypes.HelloWorld()
    assert x == "HelloWorld", f"Expected 'HelloWorld', got {x}"
def test_doNothing():
    x = test_lib.ReturnTypes.DoNothing()
    assert x is None, f"Expected None, got {x}"