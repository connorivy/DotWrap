using System;
using System.Text;
using DotWrap.Configuration;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Extensions;

public static class MethodInfoExtensions
{
    extension(ExportedMethodInfo methodInfo)
    {
        public string? GetMethodComment()
        {
            bool hasAnyXmlComments = false;
            StringBuilder commentBuilder = new();
            commentBuilder.AppendLine(@$"""""""");

            if (!string.IsNullOrEmpty(methodInfo.SummaryComment))
            {
                commentBuilder.AppendLine(
                    $"{methodInfo.SummaryComment.Trim()}".Replace("\n", $"\n")
                );
                hasAnyXmlComments = true;
            }

            if (methodInfo.Parameters.Count > 0)
            {
                commentBuilder.AppendLine($"Args:");
                foreach (var param in methodInfo.Parameters)
                {
                    if (!string.IsNullOrEmpty(param.Comment))
                    {
                        commentBuilder.AppendLine($"    {param.Name}: {param.Comment}");
                        hasAnyXmlComments = true;
                    }
                    else
                    {
                        commentBuilder.AppendLine($"    {param.Name}");
                    }
                }
            }

            if (!string.IsNullOrEmpty(methodInfo.ReturnsComment))
            {
                commentBuilder.AppendLine($"Returns:");
                commentBuilder.AppendLine($"    {methodInfo.ReturnsComment}");
                hasAnyXmlComments = true;
            }
            commentBuilder.Append(@$"""""""");

            return hasAnyXmlComments ? commentBuilder.ToString() : null;
        }
    }
}
