using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;

namespace DBF.Helpers
{
    public class SeriliazedFileSystemWatcher : FileSystemWatcher
    {
        #region private fields
            private event Func<FileSystemEventArgs, Task> _onUpdatedAsync;

            // Queue to ensure file system events are processed sequentially in the order received
            // We keep only the latest event per path so near-duplicate events are grouped.
            private readonly System.Collections.Concurrent.ConcurrentQueue<string>                           _eventQueue   = new();
            private readonly System.Collections.Concurrent.ConcurrentDictionary<string, FileSystemEventArgs> _latestEvents = new();
            private readonly System.Threading.SemaphoreSlim                                                  _queueSignal  = new(0);
            private          int                                                                             _processingEventLoop;
        #endregion

        #region Constructors
            public SeriliazedFileSystemWatcher()
            {
                base.Changed+= handleUpdate;
                base.Created+= handleUpdate;
                base.Deleted+= handleUpdate;
                base.Renamed+= handleUpdate;
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
                    _onUpdatedAsync+= value;
                }

                remove
                {
                    _onUpdatedAsync-= value;
                }
            }
        #endregion

        #region Private methods
            private void handleUpdate(object sender, FileSystemEventArgs e)
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
                        if (System.Threading.Interlocked.CompareExchange(ref _processingEventLoop, 1, 0) == 0)
                            _ = Task.Run(ProcessEventQueueAsync);
                    }
                    else
                    {
                        // Already queued; update latest event
                        _latestEvents[path] = e;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"folderUpdated error: {ex.Message}");
                    //ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                }
            }

            // Processor loop that handles queued FileSystem events one-by-one in order
            private async Task ProcessEventQueueAsync()
            {
                try
                {
                    while (true)
                    {
                        await _queueSignal.WaitAsync();

                        if (!_eventQueue.TryDequeue(out var path))
                            continue; // spurious signal

                        try
                        {
                            // Try to get and remove latest event for this path
                            if (!_latestEvents.TryRemove(path, out var ev))
                                continue; // nothing to process

                            // Wait a short time for the writer to finish and wait for size-stability
                            await Task.Delay(200);

                            long lastLength = -1;

                            for (int i = 0; i <  6; i++)
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
                                await Task.Delay(150);
                            }

                            // Execute handling on UI thread 
                            await Execute.OnUIThreadAsync(async () =>
                            {
                                await _onUpdatedAsync(ev);
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error processing queued file event: {ex.Message}");
                        }
                    }
                }

                finally
                {
                    // We never exit the loop in normal operation; ensure flag cleared if we do
                    System.Threading.Interlocked.Exchange(ref _processingEventLoop, 0);
                }
            }
        #endregion
    }
}
