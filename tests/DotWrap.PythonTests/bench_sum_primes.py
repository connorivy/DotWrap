import time
import dotwrap_testlib
from dotwrap_testlib import IsPrimeBench as bench


def is_prime(number):
    if number <= 1:
        return False
    for i in range(2, int(number**0.5) + 1):
        if number % i == 0:
            return False
    return True


def sum_of_primes():
    total = 0
    for i in range(2, 500_000):
        if is_prime(i):
            total += i
    return total


def benchmark():
    print("Benchmarking Python sum_of_primes...")
    start = time.perf_counter()
    py_result = sum_of_primes()
    py_time = time.perf_counter() - start
    print(f"Python sum_of_primes: {py_result} (Time: {py_time:.6f}s)")

    # init the cffi module here to avoid measuring import time
    # todo: we'll bake this into the import statement in the future
    dotwrap_testlib.ReturnTypesSimple.do_nothing()
    print("Benchmarking c# sum_of_primes...")
    start = time.perf_counter()
    cs_result = bench.sum_of_primes()
    cs_time = time.perf_counter() - start
    print(f"c# sum_of_primes: {cs_result} (Time: {cs_time:.6f}s)")

    print(f"C# implementation is {py_time / cs_time:.2f} times faster than Python.")


if __name__ == "__main__":
    benchmark()
