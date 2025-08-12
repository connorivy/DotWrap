import dotwrap_testlib as test_lib
import pytest


def test_invalid_operation_exception():
    instance = test_lib.Exceptions()
    with pytest.raises(test_lib.DotWrapTestLibError):
        instance.throw_invalid_operation_exception(42)
    assert True
