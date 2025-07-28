import time
from DotWrap_TestLib import IsPrimeBench as bench


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

    print("Benchmarking bench.SumOfPrimes...")
    start = time.perf_counter()
    bench_result = bench.SumOfPrimes()
    bench_time = time.perf_counter() - start
    print(f"bench.SumOfPrimes: {bench_result} (Time: {bench_time:.6f}s)")


if __name__ == "__main__":
    benchmark()
