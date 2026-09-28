using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using CodexPulse.Models;

namespace CodexPulse.Services;

public sealed class CodexAppServerClient : IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> _pending = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private Process? _process;
    private StreamWriter? _writer;
    private long _nextRequestId;
    private CancellationTokenSource? _lifetime;

    public event EventHandler<RateLimitsResponse>? RateLimitsUpdated;

    public bool IsRunning => _process is { HasExited: false };

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return;
        }

        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var startInfo = new ProcessStartInfo
        {
            FileName = "codex",
            Arguments = "app-server",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Codex app-server could not be started.");
        _writer = _process.StandardInput;

        _ = Task.Run(() => ReadOutputAsync(_process.StandardOutput, _lifetime.Token), _lifetime.Token);
        _ = Task.Run(() => ReadErrorsAsync(_process.StandardError, _lifetime.Token), _lifetime.Token);

        await SendRequestAsync<JsonElement>("initialize", new
        {
            clientInfo = new
            {
                name = "codex-pulse",
                title = "Codex Pulse",
                version = "0.1.0"
            }
        }, cancellationToken);

        await SendNotificationAsync("initialized", new { }, cancellationToken);
    }

    public async Task<RateLimitsResponse> GetRateLimitsAsync(CancellationToken cancellationToken = default)
    {
        await StartAsync(cancellationToken);
        var result = await SendRequestAsync<JsonElement>("account/rateLimits/read", null, cancellationToken);
        return result.Deserialize<RateLimitsResponse>(_jsonOptions)
            ?? throw new InvalidOperationException("Codex returned an empty rate-limit response.");
    }

    private async Task ReadOutputAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && await reader.ReadLineAsync(cancellationToken) is { } line)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                using var document = JsonDocument.Parse(line);
                var root = document.RootElement.Clone();

                if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt64(out var id))
                {
                    if (_pending.TryRemove(id, out var pending))
                    {
                        if (root.TryGetProperty("error", out var error))
                        {
                            pending.TrySetException(new InvalidOperationException(error.ToString()));
                        }
                        else if (root.TryGetProperty("result", out var result))
                        {
                            pending.TrySetResult(result.Clone());
                        }
                        else
                        {
                            pending.TrySetResult(default);
                        }
                    }
                }

                if (root.TryGetProperty("method", out var methodElement) && methodElement.GetString() == "account/rateLimits/updated" && root.TryGetProperty("params", out var parameters))
                {
                    var update = parameters.Deserialize<RateLimitsResponse>(_jsonOptions);
                    if (update is not null)
                    {
                        RateLimitsUpdated?.Invoke(this, update);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            foreach (var pending in _pending.Values)
            {
                pending.TrySetException(exception);
            }
        }
    }

    private static async Task ReadErrorsAsync(StreamReader reader, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && await reader.ReadLineAsync(cancellationToken) is not null)
            {
                // Codex diagnostics are intentionally not surfaced unless a request fails.
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task<T> SendRequestAsync<T>(string method, object? parameters, CancellationToken cancellationToken)
    {
        var id = Interlocked.Increment(ref _nextRequestId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pending[id] = completion;

        try
        {
            await WriteAsync(new { method, id, @params = parameters }, cancellationToken);
            var result = await completion.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            return result.Deserialize<T>(_jsonOptions)
                ?? throw new InvalidOperationException($"Codex returned no result for {method}.");
        }
        finally
        {
            _pending.TryRemove(id, out _);
        }
    }

    private async Task SendNotificationAsync(string method, object? parameters, CancellationToken cancellationToken)
    {
        await WriteAsync(new { method, @params = parameters }, cancellationToken);
    }

    private async Task WriteAsync(object message, CancellationToken cancellationToken)
    {
        if (_writer is null)
        {
            throw new InvalidOperationException("Codex app-server is not connected.");
        }

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await _writer.WriteLineAsync(JsonSerializer.Serialize(message, _jsonOptions));
            await _writer.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_lifetime is not null)
        {
            await _lifetime.CancelAsync();
        }

        if (_process is { HasExited: false })
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
        }

        _process?.Dispose();
        _writer?.Dispose();
        _writeLock.Dispose();
        _lifetime?.Dispose();
    }
}
