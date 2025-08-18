import time
from testlib import ReturnTypesSimple as bench


def do_nothing():
    pass


bench_range = 1_000_000


def benchmark():
    print("Benchmarking Python do_nothing...")
    start = time.perf_counter()

    for _ in range(bench_range):
        do_nothing()
    py_time = time.perf_counter() - start
    print(f"Python baseline took (Time: {py_time:.6f}s)")

    print("Benchmarking bench.DoNothing...")
    start = time.perf_counter()

    for _ in range(bench_range):
        bench.do_nothing()
    bench_time = time.perf_counter() - start
    print(f"bench.do_nothing: (Time: {bench_time:.6f}s)")

    net_overhead = bench_time - py_time
    print(f"net overhead for dotwrap calls {net_overhead:.6f}s")
    print(f"dotwrap overhead per call {net_overhead / bench_range:.9f}s per call")


if __name__ == "__main__":
    benchmark()
