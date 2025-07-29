using System;
using System.Text;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

// public class GenericClassBuilder
// {
//     private readonly StringBuilder methodBody = new();
//     private readonly StringBuilder constructor = new();

//     public string CreateDefintion(ClassBuilderContext context)
//     {
//         var originalClassName = context.ClassInfo.ClassName;
//         // split on first < and last >
//         var startIndex = originalClassName.IndexOf('<');
//         var endIndex = originalClassName.LastIndexOf('>');
//         if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
//         {
//             throw new ArgumentException(
//                 "Invalid generic class name format. Expected format: ClassName<GenericType>."
//             );
//         }
//         var genericPart = originalClassName.Substring(startIndex + 1, endIndex - startIndex - 1);
//         var className2 = originalClassName.Substring(0, startIndex);
//     }

//     public void AddClassToMainAndInitPy(ExportedMethodInfo method)
//     {
//         var context = new MethodBuilderContext(classContext, method);
//         var (returnWrapPrefix, returnWrapSuffix) = method switch
//         {
//             { OriginalType: "string" } => ($"str(CString(", "))"),
//             { OriginalType: "bool" } => ($"bool(", ")"),
//             { OriginalType: "int[]" } => ($"Collection[int](", ")"),
//             { ExposedTypeIfDifferent: not null } => (
//                 $"{method.OriginalTypePythonized}.{FromPtr}(",
//                 ")"
//             ),
//             _ => ("", ""),
//         };

//         this.GenerateSingleMethod(context, returnWrapPrefix, returnWrapSuffix);
//     }

//     public void GenerateSingleMethod(
//         MethodBuilderContext context,
//         string? resultToExportTypePrefix,
//         string? resultToExportTypeSuffix
//     )
//     {
//         var methodInfo = context.MethodInfo;
//         var cLibMethodArgs = context.GetCMethodCallArgumentsString();

//         var paramListWithHints = string.Join(
//             ", ",
//             methodInfo.Parameters.Select(p => $"{p.Name}: {p.MapOriginalTypeToPython()}")
//         );
//         var paramNames = string.Join(", ", methodInfo.Parameters.Select(p => p.Name));
//         var pyReturnType = methodInfo.MapOriginalTypeToPython();

//         int numTries = 0;
//         string methodName = methodInfo.OriginalName;
//         while (!this.methodNames.Add(methodName))
//         {
//             numTries++;
//             methodName = $"{methodInfo.OriginalName}_{numTries}";
//         }

//         var returnCall = "return ";
//         if (methodName == "Constructor")
//         {
//             methodName = "__init__";
//             pyReturnType = "None";
//             resultToExportTypePrefix = $"self.{Ptr} = ";
//             resultToExportTypeSuffix = string.Empty;
//             returnCall = string.Empty;
//         }

//         string selfMethodParameter;
//         if (methodInfo.IsStatic && methodName != "__init__")
//         {
//             mainPy.AppendLine($"    @staticmethod");
//             selfMethodParameter = string.Empty;
//         }
//         else
//         {
//             selfMethodParameter = $"self{(methodInfo.Parameters.Count > 0 ? ", " : "")}";
//         }

//         mainPy.AppendLine(
//             $"    def {methodName}({selfMethodParameter}{paramListWithHints}){$" -> {pyReturnType}"}:"
//         );

//         var docComment = methodInfo.GetMethodComment("        ");
//         if (!string.IsNullOrWhiteSpace(docComment))
//         {
//             mainPy.AppendLine(docComment);
//         }

//         mainPy.AppendLine(
//             $"        {returnCall}{resultToExportTypePrefix}{Lib}.{context.ClassContext.ClassInfo.EntryPrefix}{methodInfo.StampedName}({cLibMethodArgs}){resultToExportTypeSuffix}"
//         );

//         mainPy.AppendLine();
//     }
// }
