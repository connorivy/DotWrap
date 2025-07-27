import DotWrap_TestLib as test_lib
import numpy as np


def test_int32_array():
    x = test_lib.ReturnTypesSimpleCollection.GetInt32Array()
    assert isinstance(x, np.ndarray), f"Expected np.ndarray, got {type(x)}"
    assert x.dtype == np.int32, f"Expected dtype np.int32, got {x.dtype}"


test_int32_array()
