using System;
using System.Text;

namespace DotWrap.MSBuild.WrapperGenerators;

public static class MethodInfoExtensions
{
    public static string? GetMethodComment(this ExportedMethodInfo methodInfo, string tabString)
    {
        bool hasAnyXmlComments = false;
        StringBuilder commentBuilder = new();
        commentBuilder.AppendLine(@$"{tabString}""""""");

        if (!string.IsNullOrEmpty(methodInfo.SummaryComment))
        {
            commentBuilder.AppendLine(
                $"{tabString}{methodInfo.SummaryComment.Trim()}".Replace("\n", $"\n{tabString}")
            );
            hasAnyXmlComments = true;
        }

        if (methodInfo.Parameters.Count > 0)
        {
            commentBuilder.AppendLine($"{tabString}Args:");
            foreach (var param in methodInfo.Parameters)
            {
                if (!string.IsNullOrEmpty(param.Comment))
                {
                    commentBuilder.AppendLine($"{tabString}    {param.Name}: {param.Comment}");
                    hasAnyXmlComments = true;
                }
                else
                {
                    commentBuilder.AppendLine($"{tabString}\t{param.Name}");
                }
            }
        }

        if (!string.IsNullOrEmpty(methodInfo.ReturnsComment))
        {
            commentBuilder.AppendLine($"{tabString}Returns:");
            commentBuilder.AppendLine($"{tabString}\t{methodInfo.ReturnsComment}");
            hasAnyXmlComments = true;
        }
        commentBuilder.Append(@$"{tabString}""""""");

        return hasAnyXmlComments ? commentBuilder.ToString() : null;
    }
}
