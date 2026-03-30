  using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using Caliburn.Micro;

namespace DBF.Helpers
{
    /// <summary>
    /// FileSystemWatcher wrapper that serializes file events, debounces near-duplicate events per path,
    /// invokes subscribers on the UI thread and ensures safe subscription/unsubscription.
    /// </summary>
    public sealed class SerializedFileSystemWatcher2 : FileSystemWatcher, IDisposable
    {
        private readonly ConcurrentQueue<string>                           _eventQueue   = new();
        private readonly ConcurrentDictionary<string, FileSystemEventArgs> _latestEvents = new();
        private readonly SemaphoreSlim                                     _queueSignal  = new(0);
        private readonly object                                            _handlersLock = new();
        private readonly CancellationTokenSource                           _cts          = new();
        private Func<FileSystemEventArgs, Task>                            _updatedHandlers;
        private          int                                               _processingEventLoop;

        public SerializedFileSystemWatcher2()
        {
            base.Changed+= HandleBaseEvent;
            base.Created+= HandleBaseEvent;
            base.Deleted+= HandleBaseEvent;
            base.Renamed+= HandleBaseEvent;
        }

        /// <summary>
        /// Subscribe async handlers. Handlers will be executed on the UI thread.
        /// </summary>
        public event Func<FileSystemEventArgs, Task> UpdatedAsync
        {
            add
            {
                lock (_handlersLock)
                {
                    _updatedHandlers+= value;
                }
            }

            remove
            {
                lock (_handlersLock)
                {
                    _updatedHandlers-= value;
                }
            }
        }

        private void HandleBaseEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                var path = e.FullPath;

                if (_latestEvents.TryAdd(path, e))
                {
                    _eventQueue.Enqueue(path);
                    _queueSignal.Release();

                    if (Interlocked.CompareExchange(ref _processingEventLoop, 1, 0) == 0)
                        _ = Task.Run(ProcessEventQueueAsync, _cts.Token);
                }
                else
                {
                    // update the latest event for this path
                    _latestEvents[path] = e;
                }
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"SerializedFileSystemWatcher.HandleBaseEvent error: {ex.Message}");
            }
        }

        private async Task ProcessEventQueueAsync()
        {
            try
            {
                while (true)
                {
                    await _queueSignal.WaitAsync(_cts.Token).ConfigureAwait(false);

                    if (_cts.IsCancellationRequested)
                        break;

                    if (!_eventQueue.TryDequeue(out var path))
                        continue;

                    if (!_latestEvents.TryRemove(path, out var ev))
                        continue;

                    // small debounce to allow writer to finish and file size stabilize
                    await Task.Delay(200, _cts.Token).ConfigureAwait(false);

                    long lastLength = -1;

                    for (int i = 0; i <  6 && !_cts.IsCancellationRequested; i++)
                    {
                        long len = 0;
                        try
                        {
                            var fi = new FileInfo(path);
                            len    = fi.Exists ? fi.Length : 0;
                        }

                        catch
                        {
                            len = -1;
                        }

                        if (lastLength != -1 && lastLength == len)
                            break;

                        lastLength = len;
                        await Task.Delay(150, _cts.Token).ConfigureAwait(false);
                    }

                    // capture handlers snapshot
                    Func<FileSystemEventArgs, Task> handlers;
                    lock (_handlersLock)
                        handlers = _updatedHandlers;

                    if (handlers == null)
                        continue;

                    // invoke all handlers sequentially on UI thread and await them
                    try
                    {
                        await Execute.OnUIThreadAsync(async () =>
                        {
                            var list = handlers.GetInvocationList()
                                               .Cast<Func<FileSystemEventArgs, Task>>()
                                               .Select(h => SafeInvokeHandlerAsync(h, ev));
                            await Task.WhenAll(list).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }

                    catch (OperationCanceledException) { /* shutting down */ }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"SerializedFileSystemWatcher: handler execution failed: {ex.Message}");
                    }
                }
            }

            catch (OperationCanceledException) { /* normal on dispose */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"SerializedFileSystemWatcher.ProcessEventQueueAsync error: {ex.Message}");
            }

            finally
            {
                Interlocked.Exchange(ref _processingEventLoop, 0);
            }
        }

        private static async Task SafeInvokeHandlerAsync(Func<FileSystemEventArgs, Task> handler, FileSystemEventArgs ev)
        {
            try
            {
                await handler(ev).ConfigureAwait(false);
            }

            catch (Exception ex)
            {
                Debug.WriteLine($"SerializedFileSystemWatcher handler threw: {ex.Message}");
            }
        }

        /// <summary>
        /// Dispose watcher and stop background loop. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            // unsubscribe base events
            base.Changed-= HandleBaseEvent;
            base.Created-= HandleBaseEvent;
            base.Deleted-= HandleBaseEvent;
            base.Renamed-= HandleBaseEvent;

            // cancel processing loop
            try
            {
                _cts.Cancel();
            }

            catch { }

            // release signal to unblock queue waiter(s)
            try
            {
                _queueSignal.Release();
            }

            catch { }

            _queueSignal.Dispose();
            _cts.Dispose();
        }
    }
}
