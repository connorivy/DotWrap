using DotWrap;

namespace DotWrap.Generator.Configuration
{
    public static class DefaultMeta
    {
        public static IEnumerable<DotWrap.DotWrapExternalMethodMeta> GetDefaultMethodMeta()
        {
            yield return new DotWrap.DotWrapExternalMethodMeta(
                typeof(IList<>),
                nameof(IList<int>.Add),
                alias: "CustomAddName"
            );
            yield return new DotWrap.DotWrapExternalMethodMeta(
                typeof(IList<>),
                nameof(IList<int>.Remove)
            );
            yield return new DotWrap.DotWrapExternalMethodMeta(
                typeof(System.Array),
                "Add",
                ignore: true
            );
            yield return new DotWrap.DotWrapExternalMethodMeta(
                typeof(System.Array),
                "Remove",
                ignore: true
            );
        }

        public static IEnumerable<DotWrap.DotWrapExternalPropertyMeta> GetDefaultPropertyMeta()
        {
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(Task<>),
                nameof(Task<int>.Result)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(typeof(Task), nameof(Task.Status));
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(ValueTask<>),
                nameof(ValueTask<int>.Result)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(ValueTask<>),
                nameof(ValueTask<int>.IsFaulted)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(ValueTask<>),
                nameof(ValueTask<int>.IsCompletedSuccessfully)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(ICollection<>),
                nameof(ICollection<int>.Count)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(IReadOnlyCollection<>),
                nameof(IReadOnlyCollection<int>.Count)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(IDictionary<,>),
                nameof(IDictionary<int, int>.Keys)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(KeyValuePair<,>),
                nameof(KeyValuePair<int, int>.Key)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(KeyValuePair<,>),
                nameof(KeyValuePair<int, int>.Value)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(System.Array),
                nameof(System.Array.Length)
            );
            yield return new DotWrap.DotWrapExternalPropertyMeta(
                typeof(System.Array),
                "Count",
                PropertyType.None
            );
        }

        public static IEnumerable<DotWrap.DotWrapExternalIndexerMeta> GetDefaultIndexerMeta()
        {
            yield return new DotWrap.DotWrapExternalIndexerMeta(typeof(IList<>));
        }
    }
}
