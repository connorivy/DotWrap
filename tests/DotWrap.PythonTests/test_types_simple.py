import testlib
import pytest


def test_int_16():
    int_16_max = 32767
    result = testlib.TypesSimple.int_16(int_16_max)
    assert result == int_16_max, f"Expected {int_16_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.int_16(int_16_max + 1)


def test_u_int_16():
    u_int_16_max = 65535
    result = testlib.TypesSimple.u_int_16(u_int_16_max)
    assert result == u_int_16_max, f"Expected {u_int_16_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.u_int_16(u_int_16_max + 1)


def test_int_32():
    int_32_max = 2147483647
    result = testlib.TypesSimple.int_32(int_32_max)
    assert result == int_32_max, f"Expected {int_32_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.int_32(int_32_max + 1)


def test_u_int_32():
    u_int_32_max = 4294967295
    result = testlib.TypesSimple.u_int_32(u_int_32_max)
    assert result == u_int_32_max, f"Expected {u_int_32_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.u_int_32(u_int_32_max + 1)


def test_int_64():
    int_64_max = 9223372036854775807
    result = testlib.TypesSimple.int_64(int_64_max)
    assert result == int_64_max, f"Expected {int_64_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.int_64(int_64_max + 1)


def test_u_int_64():
    u_int_64_max = 18446744073709551615
    result = testlib.TypesSimple.u_int_64(u_int_64_max)
    assert result == u_int_64_max, f"Expected {u_int_64_max}, got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.u_int_64(u_int_64_max + 1)


def test_half():
    half_max = 65504.0
    result = testlib.TypesSimple.half(half_max)
    assert result == half_max, f"Expected {half_max}, got {result}"

    # with pytest.raises(Exception):
    #     result = testlib.TypesSimple.half(half_max + 1)
    #     print(f"Result: {result}")


def test_single():
    float_max = 3.4028234663852886e38
    result = testlib.TypesSimple.single(float_max)
    assert result == float_max, f"Expected {float_max}, got {result}"

    # with pytest.raises(Exception):
    #     result = testlib.TypesSimple.single(float_max + 1)
    #     print(f"Result: {result}")


def test_double():
    double_max = 1.7976931348623157e308
    result = testlib.TypesSimple.double(double_max)
    assert result == double_max, f"Expected {double_max}, got {result}"

    # with pytest.raises(Exception):
    #     result = testlib.TypesSimple.double(double_max + 1)
    #     print(f"Result: {result}")


def test_bool():
    result = testlib.TypesSimple.bool(True)
    assert result is True, f"Expected True, got {result}"

    result = testlib.TypesSimple.bool(False)
    assert result is False, f"Expected False, got {result}"


def test_char():
    result = testlib.TypesSimple.char("a")
    assert result == "a", f"Expected 'a', got {result}"

    with pytest.raises(Exception):
        result = testlib.TypesSimple.char("ab")
        print(f"Result: {result}")


def test_string():
    result = testlib.TypesSimple.string("hello")
    assert result == "hello", f"Expected 'hello', got {result}"
