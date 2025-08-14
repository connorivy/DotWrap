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
