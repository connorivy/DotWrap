import testlib as test_lib


def test_out_params_int():
    var_42 = test_lib.outtypes.OutInt()
    test_lib.OutParams.out_int_42(var_42)
    print("42:", var_42.value)
    assert var_42.value == 42, "Expected the output to be 42"


def test_out_params_string():
    hello = test_lib.outtypes.OutString()
    test_lib.OutParams.out_string_hello_world(hello)
    print("Hello:", hello.value)
    assert hello.value == "HelloWorld", "Expected the output to be 'HelloWorld'"
    assert hello.value == "HelloWorld", "Expected the output to be 'HelloWorld'"

def test_out_params_int_enum():
    one = test_lib.outtypes.OutMyIntEnum()
    test_lib.OutParams.out_int_enum_one(one)
    print("One:", one.value)
    assert one.value == test_lib.MyIntEnum.one, "Expected the output to be MyIntEnum.One"

def test_out_params_two_byte():
    two = test_lib.outtypes.OutMyByteEnum()
    test_lib.OutParams.out_byte_enum_two(two)
    print("Two:", two.value)
    assert two.value == test_lib.MyByteEnum.two, "Expected the output to be MyByteEnum.Two"

def test_out_params_my_class():
    cust_class = test_lib.outtypes.OutMyClass()
    test_lib.OutParams.out_custom_class(cust_class)
    print("ClassVal:", cust_class.value)
    assert cust_class.value.x == 3, "Expected the output to be 3"
    assert cust_class.value.y == 4, "Expected the output to be 4"
