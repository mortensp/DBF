using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows.Documents;
using Caliburn.Micro;

namespace DBF.Helpers
{
    /// <summary>
    /// FileSystemWatcher wrapper that serializes file events, debounces near-duplicate events per path,
    /// invokes subscribers on the UI thread and ensures safe subscription/unsubscription.
    /// </summary>
    public sealed class SerializedFileSystemWatcher : FileSystemWatcher
    {
        #region Private fields
            private readonly ConcurrentQueue<string>                           _eventQueue   = new();
            private readonly ConcurrentDictionary<string, FileSystemEventArgs> _latestEvents = new();
            private readonly SemaphoreSlim                                     _queueSignal  = new(0);
            private readonly object                                            _handlersLock = new();
            private readonly CancellationTokenSource                           _cts          = new();
            private Func<FileSystemEventArgs, Task>                            _updatedHandlers;
            private          int                                               _processingEventLoop;
        #endregion

        #region Constructors
            /// <devdoc>
            ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class.
            /// </devdoc>
            public SerializedFileSystemWatcher()
            {
                base.Changed+= HandleBaseEvent;
                base.Created+= HandleBaseEvent;
                base.Deleted+= HandleBaseEvent;
                base.Renamed+= HandleBaseEvent;
            }

            /// <devdoc>
            ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
            ///    given the specified directory to monitor.
            /// </devdoc>
            public SerializedFileSystemWatcher(string path) : this()
            {
                Path = path;
            }

            /// <devdoc>
            ///    Initializes a new instance of the <see cref='System.IO.FileSystemWatcher'/> class,
            ///    given the specified directory and type of files to monitor.
            /// </devdoc>
            public SerializedFileSystemWatcher(string path, string filter) : this()
            {
                Path   = Path;
                Filter = filter;
            }
        #endregion

        #region Public Properties
            /// <devdoc>
            ///    Occurs when a file or directory in the specified <see cref='System.IO.FileSystemWatcher.Path'/> is changed or crerated.
            /// </devdoc>
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
        #endregion

        #region Public Methods
            public bool WatchForNewFolder(string dir, string fileFilter, bool enable = true)
            {
                if (Directory.Exists(dir))
                {
                    this.Path                  = dir;
                    this.Filter                = fileFilter;
                    this.IncludeSubdirectories = false;
                    this.EnableRaisingEvents   = enable;
                    return true;
                }
                else
                {
                    this.Path                  = dir.FindDeepestExistingDirectory();
                    this.Filter                = dir.FirstNonSharedDirectory(this.Path) ?? fileFilter;
                    this.IncludeSubdirectories = true;
                    this.EnableRaisingEvents   = enable;
                    return false;
                }
            }
        #endregion

        #region Private methods
            private void HandleBaseEvent(object sender, FileSystemEventArgs e)
            {
                try
                {
                    var path = e.FullPath;

                    // Store/update the latest event for this path. If this is the first event for the path,
                    // enqueue the path so the processor will handle it. This collapses near-duplicate events
                    // for the same file into a single processing run (we keep the latest event).
                    if (_latestEvents.TryAdd(path, e))
                    {
                        _eventQueue.Enqueue(path);
                        _queueSignal.Release();

                        // Ensure single processor is running
                        if (Interlocked.CompareExchange(ref _processingEventLoop, 1, 0) == 0)
                            _ = Task.Run(ProcessEventQueueAsync, _cts.Token);
                    }
                    else
                    {
                        // Already queued; update latest event
                        _latestEvents[path] = e;
                    }
                }

                catch (Exception ex)
                {
                    Debug.WriteLine($"SerializedFileSystemWatcher.HandleBaseEvent error: {ex.Message}");
                    Debug.WriteLine($"folderUpdated error: {ex.Message}");
                }
            }

            // Processor loop that handles queued FileSystem events one-by-one in order
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
                            continue; // spurious signal

                        // Try to get and remove latest event for this path
                        if (!_latestEvents.TryRemove(path, out var ev))
                            continue; // nothing to process

                        // Wait a short time for the writer to finish and wait for size-stability
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

                        // Execute handling on UI thread 
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

                        catch (OperationCanceledException)
                        {
                            /* shutting down */
                        }

                        catch (Exception ex)
                        {
                            Debug.WriteLine($"SerializedFileSystemWatcher: handler execution failed: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    /* normal on dispose */
                }

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
            public new void Dispose()
            {
                // unsubscribe base events
                base.Changed-= HandleBaseEvent;
                base.Created-= HandleBaseEvent;
                base.Deleted-= HandleBaseEvent;
                base.Renamed-= HandleBaseEvent;
                // We never exit the loop in normal operation; ensure flag cleared if we do
                // This ensures that a new processor will be started if new events arrive after we exit.
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
        #endregion
    }
}
