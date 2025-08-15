import testlib


def test_nullable_int_with_value():
    value = testlib.NullableTypes.nullable_int(5)
    assert value == 5, f"Expected 5, but got {value}"


def test_nullable_int_without_value():
    value = testlib.NullableTypes.nullable_int(None)
    assert value is None, f"Expected None, but got {value}"


def test_nullable_string_with_value():
    value = testlib.NullableTypes.nullable_string("Hello")
    assert value == "Hello", f"Expected 'Hello', but got {value}"


def test_nullable_string_without_value():
    value = testlib.NullableTypes.nullable_string(None)
    assert value is None, f"Expected None, but got {value}"


def test_nullable_custom_class_with_value():
    value = testlib.NullableTypes.nullable_custom_class(None)
    assert value is None, f"Expected None, but got {value}"


test_nullable_custom_class_with_value()
