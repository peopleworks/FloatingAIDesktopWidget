using System.IO.Pipes;
using System.Security.Principal;

namespace FloatingAIDesktopWidget;

internal sealed class SingleInstanceManager : IDisposable
{
    private readonly Mutex _mutex;
    private readonly CancellationTokenSource _cts = new();
    private readonly string _pipeName;

    public bool IsFirstInstance { get; }

    private SingleInstanceManager(Mutex mutex, bool isFirstInstance, string pipeName)
    {
        _mutex = mutex;
        IsFirstInstance = isFirstInstance;
        _pipeName = pipeName;
    }

    public static SingleInstanceManager Create(string appId)
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var instanceId = $"{appId}_{sid}".Replace('\\', '_').Replace('/', '_').Replace(':', '_');

        var mutexName = $@"Local\{instanceId}";
        var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out var createdNew);

        var pipeName = instanceId;
        return new SingleInstanceManager(mutex, createdNew, pipeName);
    }

    public void StartServer(Action onSignal)
    {
        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(_cts.Token).ConfigureAwait(false);

                    if (_cts.IsCancellationRequested)
                    {
                        return;
                    }

                    onSignal();
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch
                {
                    await Task.Delay(250, _cts.Token).ConfigureAwait(false);
                }
            }
        }, _cts.Token);
    }

    public bool TrySignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out, PipeOptions.Asynchronous);
            client.Connect(timeout: 250);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
        }
        catch
        {
            // ignore
        }

        _cts.Dispose();

        try
        {
            _mutex.ReleaseMutex();
        }
        catch
        {
            // ignore
        }

        _mutex.Dispose();
    }
}

