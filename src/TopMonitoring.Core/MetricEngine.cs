using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace TopMonitoring.Core
{
    /// <summary>
    /// Simple metric engine that schedules providers and emits snapshots via a channel.
    /// Design goals: fault-tolerant, provider isolation, configurable poll intervals.
    /// </summary>
    public class MetricEngine : IAsyncDisposable
    {
        private readonly IEnumerable<IMetricProvider> _providers;
        private readonly CancellationTokenSource _cts = new();
        private readonly Channel<MetricSnapshot> _channel = Channel.CreateUnbounded<MetricSnapshot>();
        private readonly List<Task> _workerTasks = new();

        public MetricEngine(IEnumerable<IMetricProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        public IAsyncEnumerable<MetricSnapshot> GetSnapshotsAsync() => ReadAllAsync();

        private async IAsyncEnumerable<MetricSnapshot> ReadAllAsync()
        {
            var reader = _channel.Reader;
            while (await reader.WaitToReadAsync(_cts.Token))
            {
                while (reader.TryRead(out var item))
                {
                    yield return item;
                }
            }
        }

        public void Start()
        {
            foreach (var p in _providers)
            {
                _workerTasks.Add(Task.Run(() => WorkerLoopAsync(p, _cts.Token)));
            }
        }

        private async Task WorkerLoopAsync(IMetricProvider provider, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var snapshot = await provider.PollAsync(ct);
                        if (snapshot != null)
                            await _channel.Writer.WriteAsync(snapshot, ct);
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        // swallow provider errors but keep engine running
                        var errSnapshot = new MetricSnapshot(provider.Id, provider.Category, DateTime.UtcNow, null, $"ERROR: {ex.Message}");
                        await _channel.Writer.WriteAsync(errSnapshot, ct);
                        await Task.Delay(TimeSpan.FromSeconds(1), ct); // backoff
                    }
                    await Task.Delay(provider.PollInterval, ct);
                }
            }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try { await Task.WhenAll(_workerTasks); } catch { }
            _cts.Dispose();
            _channel.Writer.TryComplete();
            foreach (var p in _providers) await p.DisposeAsync();
        }
    }
}
