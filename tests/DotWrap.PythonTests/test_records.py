import testlib as test_lib

def test_record1():
    result = test_lib.Records1(5, 6)
    assert result.x == 5, f"Expected x to be 5, got {result.x}"
    assert result.y == 6, f"Expected y to be 6, got {result.y}"