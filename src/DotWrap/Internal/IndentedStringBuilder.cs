using System;
using System.Text;

namespace DotWrap.Internal;

internal abstract class IndentedStringBuilder
{
    private readonly StringBuilder _sb = new();
    private int _indentLevel = 0;
    private const string IndentString = "    ";

    public abstract void EnterBlock();

    public abstract void ExitBlock();

    public IDisposable IndentUntilDispose()
    {
        return new StringBuilderIndenter(this);
    }

    protected void IncreaseIndent()
    {
        _indentLevel++;
    }

    protected void DecreaseIndent()
    {
        _indentLevel--;
    }

    public override string ToString()
    {
        return _sb.ToString();
    }

    /// <summary>
    /// Find each line in the input string and prepend the current indent level to it,
    /// then append to the internal StringBuilder
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    public IndentedStringBuilder AppendLine(string line = "")
    {
        var indent = new string(' ', _indentLevel * IndentString.Length);
        var lines = (line ?? string.Empty).Split(
            new[] { "\r\n", "\n", "\r" },
            StringSplitOptions.None
        );

        foreach (var l in lines)
        {
            _sb.Append(indent);
            _sb.AppendLine(l);
        }
        // If the input was empty, still append an indented empty line
        if (lines.Length == 0)
        {
            _sb.AppendLine(indent);
        }
        return this;
    }

    public IDisposable AppendLineWithNewBlock(string line)
    {
        AppendLine(line);
        return new StringBuilderIndenter(this);
    }

    public IndentedStringBuilder Append(string text)
    {
        _sb.Append(new string(' ', _indentLevel * IndentString.Length) + text);
        return this;
    }
}

internal class IndentedCSharpStringBuilder : IndentedStringBuilder
{
    public override void EnterBlock()
    {
        AppendLine("{");
        IncreaseIndent();
    }

    public override void ExitBlock()
    {
        DecreaseIndent();
        AppendLine("}");
    }
}

internal class IndentedPythonStringBuilder : IndentedStringBuilder
{
    public override void EnterBlock()
    {
        IncreaseIndent();
    }

    public override void ExitBlock()
    {
        DecreaseIndent();
    }
}

internal class StringBuilderIndenter : IDisposable
{
    private readonly IndentedStringBuilder _sb;

    public StringBuilderIndenter(IndentedStringBuilder sb)
    {
        _sb = sb;
        _sb.EnterBlock();
    }

    public void Dispose()
    {
        _sb.ExitBlock();
    }
}
