using DotWrap.Configuration.Python;

internal class TaskConfig : DotWrapPythonTypeConfig
{
    public override Type TypeToConfigure => typeof(Task);

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

internal class ValueTaskConfig : DotWrapPythonTypeConfig
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
