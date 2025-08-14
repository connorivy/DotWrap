import testlib as test_lib


def test_namespace_overrides():
    # i configured builtin.threading to override system.threading.tasks.
    # if this test can find the TaskStatus type in the configured namespace,
    # then the namespace alias logic is working
    x = test_lib.builtin.threading.TaskStatus.running
    assert x.value == 3, f"Expected TaskStatus.running to be 3, got {x.value}"


def test_external_namespace_alias():
    # i configured day of week with a namespace alias of dow.namespace.alias
    # if this test can find dayOfWeek in that namespace, then it is working
    x = test_lib.dow.namespace.alias.DayOfWeek.monday
    assert x.value == 1, f"Expected sunday to be 1, got {x.value}"


def test_external_type_alias():
    # i configured TypeCode with the alias TypeCodeAlias
    # if this test can find TypeCodeAlias, then it is working
    x = test_lib.system.TypeCodeAlias.object
    assert x.value == 1, f"Expected object to be 1, got {x.value}"
