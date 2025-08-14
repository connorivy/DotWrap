using System.Runtime.CompilerServices;
using DiffEngine;
using VerifyTests;

namespace DotWrap.Tests;

public static class GlobalSetup
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        VerifySourceGenerators.Initialize();
        DiffTools.UseOrder(DiffTool.VisualStudioCode);
    }
}
