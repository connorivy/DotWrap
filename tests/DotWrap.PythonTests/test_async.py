import pytest
import testlib as test_lib


def test_task_of_int_result():
    result = test_lib.Async.task_of_42()
    assert result.result == 42


def test_value_task_of_int_result():
    result = test_lib.Async.value_task_of_55()
    assert result.result == 55


@pytest.mark.asyncio
async def test_task_of_int_await():
    result = await test_lib.Async.task_of_42()
    assert result == 42


@pytest.mark.asyncio
async def test_value_task_of_int_await():
    result = await test_lib.Async.value_task_of_55()
    assert result == 55
