import testlib as test_lib

def test_external_dependent_methods_appear_on_generated_wrapper():
    parentClass = test_lib.HasExternalDependency()
    five = parentClass.return_5_from_parent_class()
    assert five == 5, f"Expected 5, got {five}"

    ten = parentClass.return_10_from_dependency()
    assert ten == 10, f"Expected 10, got {ten}"

def test_class_should_be_exposed_by_default():
    parentClass = test_lib.dependencylib.ClassShouldBeExposedByDefault()
    five = parentClass.get_five()
    assert five == 5, f"Expected 5, got {five}"

    ten = parentClass.get_ten()
    assert ten == 10, f"Expected 10, got {ten}"