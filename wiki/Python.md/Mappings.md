# C# To Python Mappings table

| C# Type(s)                         | Python Equivalent       | C# Method / Property | Python Method / Property |
|------------------------------------|-------------------------|----------------------|--------------------------|
| `List<T>`                          | `MutableSequence[T]`    | `Add`                | `append`                 |
| `IList<T>`                         |                         | `Insert`             | `insert`                 |
|                                    |                         | `Remove`             | `remove`                 |
|                                    |                         | `Count`              | `len()`                  |
|                                    |                         | `Contains`           | `in`                     |
|                                    |                         | `Clear`              | `clear`                  |
|                                    |                         | Indexer `[i]`        | `[i]` (get/set)           |
| `ReadOnlyList<T>`                  | `Sequence[T]`           | `Count`              | `len()`                  |
|                                    |                         | `Contains`           | `in`                     |
|                                    |                         | Indexer `[i]`        | `[i]` (get only)          |
| `T[]` (Array)                      | `Sequence[T]` (fixed)   | `Length`             | `len()`                  |
|                                    |                         | Indexer `[i]`        | `[i]` (get/set)           |
| `Dictionary<TKey, TValue>`         | `MutableMapping[K, V]`  | `Add`                | `d[k] = v`               |
|                                    |                         | `Remove`             | `del d[k]`               |
|                                    |                         | `ContainsKey`        | `in d`                   |
|                                    |                         | `Count`              | `len()`                  |
|                                    |                         | `Keys`               | `d.keys()`               |
|                                    |                         | `Values`             | `d.values()`             |
|                                    |                         | `TryGetValue`        | `d.get(k)`               |
|                                    |                         | Indexer `[k]`        | `[k]` (get/set)           |
| `ReadOnlyDictionary<K, V>`         | `Mapping[K, V]`         | `Count`              | `len()`                  |
|                                    |                         | `Keys`               | `d.keys()`               |
|                                    |                         | `Values`             | `d.values()`             |
|                                    |                         | `ContainsKey`        | `in d`                   |
|                                    |                         | Indexer `[k]`        | `[k]` (get only)          |
| `IEnumerable<T>`                   | `Iterable[T]`           | `GetEnumerator`      | `iter()`                 |
|                                    |                         | `foreach`            | `for ... in`             |
| `HashSet<T>`                       | `MutableSet[T]`         | `Add`                | `add`                    |
|                                    |                         | `Remove`             | `remove`                 |
|                                    |                         | `Contains`           | `in`                     |
|                                    |                         | `UnionWith`          | `|=`                      |
|                                    |                         | `IntersectWith`      | `&=`                      |
|                                    |                         | `ExceptWith`         | `-=`                      |
|                                    |                         | `Count`              | `len()`                  |
| `IReadOnlySet<T>`                  | `Set[T]`                | `Contains`           | `in`                     |
|                                    |                         | `Count`              | `len()`                  |
