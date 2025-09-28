import testlib
import pytest

def test_list_of_string():
    input_list = ["apple", "banana", "cherry"]
    result = testlib.TypesSimpleCollections.list_of_string(input_list)
    assert result == input_list, f"Expected {input_list}, got {result}"