import DotWrap_TestLib as test_lib
import numpy as np


def test_int32_array():
    x = test_lib.ReturnTypesSimpleCollection.GetInt32Array()
    y = x.to_list()
    assert isinstance(x, test_lib.Collection), f"Expected Collection, got {type(x)}"
    assert x[0] == y[0], f"Expected {x[0]}, got {y[0]}"


test_int32_array()
