using DotWrap;
using DotWrap.Configuration;
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
    public static Task<int> TaskOf42()
    {
        // Using Task.FromResult to avoid actual async behavior in CI
        // which might be causing AOT compilation issues
        return Task.FromResult(42);
    }

    public static ValueTask<int> ValueTaskOf55()
    {
        // Using ValueTask with direct result to avoid async timing issues
        return new ValueTask<int>(55);
    }
}

public class TaskConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(Task);

    public override void ConfigureImports(IndentedStringBuilder sb)
    {
        sb.AppendLine("import asyncio");
    }

    public override void ConfigureGenericClassBody(PythonTypeConfigContext context)
    {
        var genericClassBodyBuilder = context.ClassBody;
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

    public override void ConfigureGenericClassBody(PythonTypeConfigContext context)
    {
        var genericClassBodyBuilder = context.ClassBody;

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
