import dotwrap_testlib as test_lib


def test_int_32_array_tolist():
    x = test_lib.ReturnTypesSimpleCollection.get_int_32_array()
    y = x.to_list()

    assert y == [2147483647, -2147483648], (
        f"Expected list [2147483647, -2147483648], got {y}"
    )


def test_int_32_array_indexer():
    x = test_lib.ReturnTypesSimpleCollection.get_int_32_array()

    assert x[0] == 2147483647, f"Expected 2147483647, got {x[0]}"
    assert x[1] == -2147483648, f"Expected -2147483648, got {x[1]}"

    x[0] = 100
    x[1] = -100
    assert x[0] == 100, f"Expected 100, got {x[0]}"
    assert x[1] == -100, f"Expected -100, got {x[1]}"


def test_int_32_list():
    x = test_lib.ReturnTypesSimpleCollection.get_int_32_list()
    y = x.to_list()
    assert y == [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], (
        f"Expected list [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], got {y}"
    )


def test_read_only_int_32_list():
    x = test_lib.ReturnTypesSimpleCollection.get_read_only_int_32_list()
    y = x.to_list()
    assert y == [0, 1, 2, 3, 4, 5], f"Expected list [0, 1, 2, 3, 4, 5], got {y}"


def test_int_to_string_dict_keys():
    x = test_lib.ReturnTypesSimpleCollection.get_int_string_dictionary()
    y = x.keys.to_list()
    assert y == [1, 2, 3], f"Expected keys [1, 2, 3], got {y}"


def test_int_to_string_dict():
    x = test_lib.ReturnTypesSimpleCollection.get_int_string_dictionary()
    y = x.to_list()

    assert y[0].key == 1 and y[0].value == "One"
    assert y[1].key == 2 and y[1].value == "Two"
    assert y[2].key == 3 and y[2].value == "Three"


def test_int_to_long_dict():
    x = test_lib.ReturnTypesSimpleCollection.get_int_long_dictionary()
    y = x.to_list()

    assert y[0].key == 1 and y[0].value == 10000000000
    assert y[1].key == 2 and y[1].value == 20000000000
    assert y[2].key == 3 and y[2].value == 30000000000


# test_int_32_array()
# test_int_32_list()
# test_int_to_string_dict()
# test_int_to_string_dict_keys()
