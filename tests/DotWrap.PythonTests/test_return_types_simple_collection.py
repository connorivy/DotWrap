import DotWrap_TestLib as test_lib
import numpy as np


# def test_int32_array():
#     x = test_lib.ReturnTypesSimpleCollection.get_int32_array()
#     y = x.to_list()
#     a = x.element_at(0)
#     z = y[0]
#     y[0] = 42
#     a = 55
#     # assert isinstance(x, test_lib.Collection), f"Expected Collection, got {type(x)}"
#     # assert x[0] == y[0], f"Expected {x[0]}, got {y[0]}"


def test_int32_list():
    x = test_lib.ReturnTypesSimpleCollection.get_int32_list()
    y = x.to_list()
    assert y == [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], (
        f"Expected list [0, 1, 2, 3, 4, 5, 6, 7, 8, 9], got {y}"
    )


def test_read_only_int32_list():
    x = test_lib.ReturnTypesSimpleCollection.get_read_only_int32_list()
    y = x.to_list()
    assert y == [0, 1, 2, 3, 4, 5], f"Expected list [0, 1, 2, 3, 4, 5], got {y}"


def test_int_to_string_dict_keys():
    x = test_lib.ReturnTypesSimpleCollection.get_int_string_dictionary()
    y = x.keys.to_list()
    assert y == [1, 2, 3], f"Expected keys [1, 2, 3], got {y}"


def test_int_to_string_dict():
    x = test_lib.ReturnTypesSimpleCollection.get_int_string_dictionary()
    y = x.to_list()
    for item in y:
        z = item.key
        a = item.value
    assert y == {1: "one", 2: "two"}, (
        f"Expected dict {{0: 'zero', 1: 'one', 2: 'two'}}, got {y}"
    )


# test_int32_array()
# test_int32_list()
# test_int_to_string_dict()
test_int_to_string_dict_keys()
