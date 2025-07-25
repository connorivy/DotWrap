import DotWrap_SampleAotLib as test_lib


def test_instance_returns_42():
    instance = test_lib.SampleClass()
    result = instance.InstanceReturn42()
    assert result == 42


def test_instance_returns_pi():
    instance = test_lib.SampleClass()
    result = instance.InstanceReturnPi()
    assert abs(result - 3.141592653589793) < 1e-9


def test_static_returns_pi():
    result = test_lib.SampleClass.StaticReturnPi()
    assert abs(result - 3.141592653589793) < 1e-9


def test_instance_returns_hello_world():
    instance = test_lib.SampleClass()
    result = instance.InstanceReturnHelloWorld()
    assert str(result) == "HelloWorld"


def test_instance_takes_int():
    instance = test_lib.SampleClass()
    result = instance.InstanceTakesInt(10)
    assert result == 10


def test_static_takes_int():
    result = test_lib.SampleClass.StaticTakesInt(20)
    assert result == 20


test_instance_returns_42()
test_instance_returns_pi()
test_instance_returns_hello_world()
