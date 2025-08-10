import dotwrap_testlib as test_lib
import numpy as np


def test_static_hello_world():
    result = test_lib.Properties.get_static_hello_world()
    assert result == "HelloWorld", f"Expected 'HelloWorld', got {result}"


def test_instance_hello_world():
    instance = test_lib.Properties()
    result = instance.instance_hello_world
    assert result == "HelloWorld", f"Expected 'HelloWorld', got {result}"


def test_get_and_set_property():
    test_lib.Properties.set_get_and_set_property(999)
    result = test_lib.Properties.get_get_and_set_property()
    assert result == 999, f"Expected 999, got {result}"
