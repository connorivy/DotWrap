import dotwrap_testlib as test_lib


def test_enum_value_two():
    instance = test_lib.ClassWithEnums()
    result = instance.get_enum(test_lib.TestEnum.value_two_no_number)
    assert int(result.value) == 2

def test_enum_value_five():
    instance = test_lib.ClassWithEnums()
    result = instance.get_value_five()
    assert int(result.value) == 5