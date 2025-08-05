using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace DotWrap.Tests;

public static class GlobalSetup
{
    [ModuleInitializer]
    public static void Initialize()
    {
        VerifySourceGenerators.Initialize();
        DiffTools.UseOrder(DiffTool.VisualStudioCode);
    }
}
