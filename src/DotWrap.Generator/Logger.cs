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
                    $"[ERROR] {DateTime.Now:HH:mm:ss.fff}: {message}",
                    "DotWrap",
                    DiagnosticSeverity.Error,
                    isEnabledByDefault: true,
                    description: "Error from DotWrap source generator"
                ),
                Location.None
            )
        );
    }
}
