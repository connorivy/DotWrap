from DotWrap_TestLib import ReturnTypesSimple as test_lib


def test_max_s_byte():
    x = test_lib.max_s_byte()
    assert x == 127, f"Expected 127, got {x}"


def test_min_s_byte():
    x = test_lib.min_s_byte()
    assert x == -128, f"Expected -128, got {x}"


def test_max_byte():
    x = test_lib.max_byte()
    assert x == 255, f"Expected 255, got {x}"


def test_min_byte():
    x = test_lib.min_byte()
    assert x == 0, f"Expected 0, got {x}"


def test_max_int16():
    x = test_lib.max_int16()
    assert x == 32767, f"Expected 32767, got {x}"


def test_min_int16():
    x = test_lib.min_int16()
    assert x == -32768, f"Expected -32768, got {x}"


def test_max_uint16():
    x = test_lib.max_u_int16()
    assert x == 65535, f"Expected 65535, got {x}"


def test_min_uint16():
    x = test_lib.min_u_int16()
    assert x == 0, f"Expected 0, got {x}"


def test_max_int32():
    x = test_lib.max_int32()
    assert x == 2147483647, f"Expected 2147483647, got {x}"


def test_min_int32():
    x = test_lib.min_int32()
    assert x == -2147483648, f"Expected -2147483648, got {x}"


def test_max_u_int32():
    x = test_lib.max_u_int32()
    assert x == 4294967295, f"Expected 4294967295, got {x}"


def test_min_u_int32():
    x = test_lib.min_u_int32()
    assert x == 0, f"Expected 0, got {x}"


def test_max_int64():
    x = test_lib.max_int64()
    assert x == 9223372036854775807, f"Expected 9223372036854775807, got {x}"


def test_min_int64():
    x = test_lib.min_int64()
    assert x == -9223372036854775808, f"Expected -9223372036854775808, got {x}"


def test_max_u_int64():
    x = test_lib.max_u_int64()
    assert x == 18446744073709551615, f"Expected 18446744073709551615, got {x}"


def test_min_u_int64():
    x = test_lib.min_u_int64()
    assert x == 0, f"Expected 0, got {x}"


def test_max_half():
    x = test_lib.max_half()
    assert x == 65504, f"Expected 65504, got {x}"


def test_min_half():
    x = test_lib.min_half()
    assert x == -65504, f"Expected -65504, got {x}"


def test_max_single():
    x = test_lib.max_single()
    result = abs(x - 3.40282346638528859811704183484516925440e38)
    assert result < 1e-6, f"Expected close to 3.4028235e38, got {x}"


def test_min_single():
    x = test_lib.min_single()
    assert abs(x + 3.40282346638528859811704183484516925440e38) < 1e-6, (
        f"Expected close to -3.4028235e38, got {x}"
    )


def test_max_double():
    x = test_lib.max_double()
    assert abs(x - 1.7976931348623157e308) < 1e-9, (
        f"Expected close to 1.7976931348623157e308, got {x}"
    )


def test_min_double():
    x = test_lib.min_double()
    assert abs(x + 1.7976931348623157e308) < 1e-9, (
        f"Expected close to -1.7976931348623157e308, got {x}"
    )


# def test_true():
#     x = test_lib.True()
#     assert x == True, f"Expected True, got {x}"
# def test_false():
#     x = test_lib.False()
#     assert x == False, f"Expected False, got {x}"
def test_hello_world():
    x = test_lib.hello_world()
    assert x == "HelloWorld", f"Expected 'HelloWorld', got {x}"


def test_do_nothing():
    x = test_lib.do_nothing()
    assert x is None, f"Expected None, got {x}"
