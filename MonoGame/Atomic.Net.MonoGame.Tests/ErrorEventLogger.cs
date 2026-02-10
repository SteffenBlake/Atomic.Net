using Atomic.Net.MonoGame.Core;
using Atomic.Net.MonoGame.BED;
using Xunit.Abstractions;

namespace Atomic.Net.MonoGame.Tests;

/// <summary>
/// Global error event logger that captures and logs all ErrorEvents during tests.
/// This helps identify swallowed JsonExceptions and other errors.
/// </summary>
public sealed class ErrorEventLogger : IEventHandler<ErrorEvent>, IDisposable
{
    private readonly ITestOutputHelper? _output;
    private readonly List<string> _errors = [];

    public ErrorEventLogger(ITestOutputHelper? output = null)
    {
        _output = output;
        EventBus<ErrorEvent>.Register(this);
    }

    public void OnEvent(ErrorEvent e)
    {
        var message = $"[ERROR EVENT] {e.Message}";
        _errors.Add(message);

        // Log to test output if available
        _output?.WriteLine(message);

        // Also log to console so it shows up in test results
        Console.WriteLine(message);
    }

    public IReadOnlyList<string> Errors => _errors;

    public void Clear()
    {
        _errors.Clear();
    }

    public void Dispose()
    {
        EventBus<ErrorEvent>.Unregister(this);
    }
}

/// <summary>
/// Test event logger that captures and logs both ErrorEvents and DebugEvents during tests.
/// DebugEvents are for temporary debugging only and MUST be removed before committing.
/// </summary>
public sealed class TestEventLogger : IEventHandler<ErrorEvent>, IEventHandler<DebugEvent>, IDisposable
{
    private readonly ITestOutputHelper _output;

    public TestEventLogger(ITestOutputHelper output)
    {
        _output = output;
        EventBus<ErrorEvent>.Register(this);
        EventBus<DebugEvent>.Register(this);
    }

    public void OnEvent(ErrorEvent e)
    {
        _output.WriteLine($"[ERROR] {e.Message}");
    }

    public void OnEvent(DebugEvent e)
    {
        _output.WriteLine($"[DEBUG] {e.Message}");
    }

    public void Dispose()
    {
        EventBus<ErrorEvent>.Unregister(this);
        EventBus<DebugEvent>.Unregister(this);
    }
}
