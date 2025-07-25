import DotWrap_TestLib as test_lib


def test_max_int64():
    x = test_lib.ReturnTypes.MaxInt64()
    assert x == 9223372036854775807


test_max_int64()
