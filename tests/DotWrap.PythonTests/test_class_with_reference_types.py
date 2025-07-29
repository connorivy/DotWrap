import DotWrap_SampleAotLib as test_lib


def test_class_with_reference_types():
    class_instance = test_lib.ClassWithReferenceTypes()
    assert class_instance is not None


def test_pass_sample_class():
    sample_class_instance = test_lib.SampleClass()
    class_instance = test_lib.ClassWithReferenceTypes()
    result = class_instance.AcceptSampleClass(sample_class_instance)
    assert result is True


def test_return_sample_class():
    class_instance = test_lib.ClassWithReferenceTypes()
    sample_class_instance = class_instance.CreateSampleClass()
    assert sample_class_instance is not None
    assert sample_class_instance.InstanceReturn42() == 42
    assert sample_class_instance.InstancePi() == 3.141592653589793


test_class_with_reference_types()
