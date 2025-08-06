import DotWrap_TestLib as test_lib


def test_enum_value_two():
    instance = test_lib.ClassWithEnums()
    result = instance.get_enum(test_lib.TestEnum.value_two_no_number)
    assert int(result.value) == 2
