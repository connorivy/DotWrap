namespace DotWrap.MSBuild.WrapperGenerators.Python;

public static class PythonUtils
{
    /// <summary>
    /// Converts c# class name to a Pythonic class name in a few different ways.
    /// - Changes List<int> to ListOfInt
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    public static string PythonizeClassName(string className)
    {
        while (className.Contains('<'))
        {
            // split on first < and last >
            var startIndex = className.IndexOf('<');
            var endIndex = className.LastIndexOf('>');
            if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
            {
                break; // no valid generic type found
            }
            var genericPart = className.Substring(startIndex + 1, endIndex - startIndex - 1);
            className =
                className.Substring(0, startIndex)
                + $"Of{genericPart}"
                + className.Substring(endIndex + 1);
        }

        return className;
    }
}
