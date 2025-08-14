using System;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace DotWrap.Generator;

internal static partial class Logger
{
    internal static SourceProductionContext Context { get; set; }

    public static void LogDebug(string message)
    {
        Context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DW0001",
                    "DotWrap Debug",
                    $"[DEBUG] {DateTime.Now:HH:mm:ss.fff}: {message}",
                    "DotWrap",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true,
                    description: "Debug information from DotWrap source generator"
                ),
                Location.None
            )
        );
    }

    public static void LogInfo(string message)
    {
        Context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DW0002",
                    "DotWrap Info",
                    $"[INFO] {DateTime.Now:HH:mm:ss.fff}: {message}",
                    "DotWrap",
                    DiagnosticSeverity.Info,
                    isEnabledByDefault: true,
                    description: "Information from DotWrap source generator"
                ),
                Location.None
            )
        );
    }

    public static void LogWarning(string message)
    {
        Context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DW0003",
                    "DotWrap Warning",
                    $"[WARN] {DateTime.Now:HH:mm:ss.fff}: {message}",
                    "DotWrap",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true,
                    description: "Warning from DotWrap source generator"
                ),
                Location.None
            )
        );
    }

    public static void LogError(string message)
    {
        Context.ReportDiagnostic(
            Diagnostic.Create(
                new DiagnosticDescriptor(
                    "DW0004",
                    "DotWrap Error",
                    $@"[ERROR] {DateTime.Now:HH:mm:ss.fff}: {{0}}",
                    "DotWrap",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "Error from DotWrap source generator"
                ),
                Location.None,
                messageArgs: message
            )
        );
    }

    public static void LogException(Exception ex)
    {
        var now = DateTime.Now.ToString("HH:mm:ss.fff");
        void Emit(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg))
                return;
            Context.ReportDiagnostic(
                Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "DW0004",
                        "DotWrap Error",
                        "{0}",
                        "DotWrap",
                        DiagnosticSeverity.Error,
                        isEnabledByDefault: true,
                        description: "Error from DotWrap source generator"
                    ),
                    Location.None,
                    messageArgs: msg
                )
            );
        }

        Emit($"[ERROR] {now} An exception occurred while generating the native interface:");
        Emit($"Message: {ex.Message}");

        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            var stackLines = ex.StackTrace.Split(
                new[] { "\r\n", "\n", "\r" },
                StringSplitOptions.RemoveEmptyEntries
            );
            foreach (var s in stackLines)
                Emit($"StackTrace: {s}");
        }

        if (ex.InnerException != null)
        {
            Emit($"InnerExceptionMessage: {ex.InnerException.Message}");
            if (!string.IsNullOrEmpty(ex.InnerException.StackTrace))
            {
                var innerStackLines = ex.InnerException.StackTrace.Split(
                    new[] { "\r\n", "\n", "\r" },
                    StringSplitOptions.RemoveEmptyEntries
                );
                foreach (var s in innerStackLines)
                    Emit($"InnerExceptionStackTrace: {s}");
            }
        }
    }
}
