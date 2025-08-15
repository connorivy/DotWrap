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
