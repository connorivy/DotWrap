import DotWrap_TestLib as test_lib
import numpy as np


def test_int32_array():
    x = test_lib.ReturnTypesSimpleCollection.GetInt32Array()
    y = x.to_list()
    a = x.element_at(0)
    z = y[0]
    y[0] = 42
    a = 55
    # assert isinstance(x, test_lib.Collection), f"Expected Collection, got {type(x)}"
    # assert x[0] == y[0], f"Expected {x[0]}, got {y[0]}"


def test_int32_list():
    x = test_lib.ReturnTypesSimpleCollection.GetInt32List()
    y = x.to_list()
    a = x.element_at(0)
    z = y[0]
    y[0] = 42
    a = 55
    # assert isinstance(x, test_lib.Collection), f"Expected Collection, got {type(x)}"
    # assert x[0] == y[0], f"Expected {x[0]}, got {y[0]}"


# test_int32_array()
test_int32_list()
