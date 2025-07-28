
using System.Collections;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class AttributeDataExtensions
{
    extension(AttributeData attrData)
    {
        public T? GetCtorArg<T>(int index, string name)
            where T : class
        {
            if (attrData.ConstructorArguments.Length <= index)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for constructor arguments.");
            }

            var arg = attrData.NamedArguments.FirstOrDefault(n => n.Key == name).Value.Value ?? attrData.ConstructorArguments[index].Value;
            if (arg is T value)
            {
                return value;
            }

            if (arg is null)
            {
                return null;
            }

            throw new InvalidCastException($"Cannot cast argument '{name}' to type '{typeof(T).Name}'.");
        }

        public T GetCtorArgForCollection<T>(int index, string name)
            // where T : IList
        {
            if (attrData.ConstructorArguments.Length <= index)
            {
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range for constructor arguments.");
            }

            var arg = attrData.NamedArguments.FirstOrDefault(n => n.Key == name).Value.Value ?? attrData.ConstructorArguments[index].Values;
            if (arg is T value)
            {
                return value;
            }

            throw new InvalidCastException($"Cannot cast argument '{name}' to type '{typeof(T).Name}'.");
        }
    }
}
