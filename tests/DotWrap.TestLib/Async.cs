using System;
using System.Reflection.Metadata;
using DotWrap;
using DotWrap.Configuration;
using DotWrap.MSBuild;
using DotWrap.Utils;

[assembly: DotWrapExternalPropertyMeta(typeof(Task<>), nameof(Task<>.Result))]
[assembly: DotWrapExternalPropertyMeta(typeof(Task), nameof(Task.Status))]
[assembly: DotWrapExternalPropertyMeta(typeof(ValueTask<>), nameof(ValueTask<>.Result))]
[assembly: DotWrapExternalPropertyMeta(typeof(ValueTask<>), nameof(ValueTask.IsFaulted))]
[assembly: DotWrapExternalPropertyMeta(
    typeof(ValueTask<>),
    nameof(ValueTask.IsCompletedSuccessfully)
)]

// [assembly: DotWrapExternalMethodMeta(typeof(Task<>), nameof(Task<>.ContinueWith))]

namespace DotWrap.TestLib;

[DotWrapExpose]
public class Async
{
    public static async Task<int> TaskOf42()
    {
        await Task.Delay(500); // Simulate some asynchronous work
        return 42; // Return a result after the delay
    }

    public static async ValueTask<int> ValueTaskOf55()
    {
        await Task.Delay(500); // Simulate some asynchronous work
        return 55; // Return a result after the delay
    }
}

public class TaskConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(Task);

    public override void ConfigureImports(IndentedStringBuilder sb)
    {
        sb.AppendLine("import asyncio");
    }

    public override void ConfigureGenericClassBody(
        ExportedTypeDefinitionInfo exportedType,
        Type mathchingType,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        genericClassBodyBuilder?.AppendLine(
            @"
def __await__(self):
    return self._poll().__await__()

async def _poll(self):
    while True:
        status = self.status
        if status == TaskStatus.ran_to_completion:
            return self.result
        elif status == TaskStatus.faulted:
            raise RuntimeError(""Error polling task"")
        await asyncio.sleep(0.1)
        "
        );
    }
}

public class ValueTaskConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(ValueTask<>);

    public override void ConfigureGenericClassBody(
        ExportedTypeDefinitionInfo exportedType,
        Type matchingType,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        genericClassBodyBuilder?.AppendLine(
            @"
def __await__(self):
    return self._poll().__await__()

async def _poll(self):
    while True:
        if self.is_completed_successfully:
            return self.result
        elif self.is_faulted:
            raise RuntimeError(""Error polling task"")
        await asyncio.sleep(0.1)
        "
        );
    }
}
