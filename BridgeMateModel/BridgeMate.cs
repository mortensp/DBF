using System.IO;
using Caliburn.Micro;
using DBF.BridgeMateModel;
using DBF.DataModel;
using DBF.Helpers;
using Microsoft.EntityFrameworkCore;
using Syncfusion.Data.Extensions;

namespace DBF;

// As long as the right database fiel isn't found a FilerWatcher is used to monitor the folder for changes.
// Once the *.bws file is found a Timer is used to prope for changes
public class BridgeMate : PropertyChangedBase
{
    #region Private Properties and Fields
        private          CancellationTokenSource     _cts     = new();
        private          Task                        _pollingTask;
        private          SerializedFileSystemWatcher watcher;
        private readonly Configuration               Configuration;
        private          string                      bwsFile;
        private          int                         bmClubNo = -1;
        private          DateTime                    lastDate;
        //
        // Cache the sections  and rounds grouped by (Section, Round) for efficient lookups
        // These Collections are initiated along side BMRounds and doesn't change until a new file is opened.
        // But properties on the items may change!
        private Dictionary<(short Section, short Round), BMRound[]> Rounds;
        private Dictionary<short, SectionInfo>                      Sections;
        private Session                                             Session;
    #endregion

    #region Constructors
        public BridgeMate(Configuration _configuration)
        {
            Configuration = _configuration;
            //
            watcher              = new();
            watcher.UpdatedAsync+= handleFileEventAsync;
            initWatcher(Configuration.BridgeMatePath, false);
        }
    #endregion

    #region Public Properties
        public BindableCollectionExt<RoundStatus> RoundStatus { get; private set; } = new();
    #endregion

    public void CheckOrOpen(DateTime playingTime, int clubNumber)
    {
        if (!Configuration.ReadBridgeMate
        ||  clubNumber <= 0)
            return;

        Logger.Info($"");
        Logger.Info($"CheckOrOpen called playingTime={playingTime} clubNumber={clubNumber}");

        lastDate = playingTime;
        stopPolling();

        var path   = $"{Configuration.BridgeMatePath}{clubNumber}\\";
        var after  = playingTime.AddDays(-14);
        var before = playingTime.AddDays(+1);

        if (!Directory.Exists(path))
        {
            Logger.Info($"BridgeMate: Directory not found: {path}");
            MessageBox.Show($"{Lex.Folder}: '{path}' {Lex.DoNotExist}. {Lex.BridgeMateError}", Lex.Error);
            return;
        }

        var fileInfos = Directory.GetFiles(path, "*.bws")
                                 .Select(path => new FileInfo(path))
                                 .Where(f =>  f.LastWriteTime >= after
                                          &&  f.CreationTime  <= before)
                                 .ToList();

        if (!fileInfos.Any())
        {
            Logger.Info($"BridgeMate: file not found for date {lastDate} in path {path}");
            Close();
            initWatcher(path, Configuration.ReadBridgeMate);
            return;
        }

        foreach (var fileInfo in fileInfos.Reverse<FileInfo>())
            try
            {
                using var db = new BridgeMateContext(fileInfo.FullName);

                Session = db.Sessions.ToList()
                                     .FirstOrDefault(s => s.Date == playingTime);

                if (Session        == null
                ||  Session.Status == 2)
                    continue;

                this.bwsFile = fileInfo.FullName;
                bmClubNo     = clubNumber;

                Rounds = db.BMRounds
                           .Include(b => b.SectionEntity) // used to get the missing pair for the section
                           .ToList()
                           .GroupBy(r => (r.Section, r.Round))
#if DEBUG
                           // For debugging purposes, we want to see the rounds in order of Section and Round
                           .OrderBy(g => g.Key.Section)
                           .ThenBy(g => g.Key.Round)
#endif
                           .ToDictionary(g => g.Key
                                        , g => g.OrderBy(r => r.TableNo)
                                                .ToArray());

                Sections = db.Sections.ToDictionary(s => s.Id
                                                   , s => new SectionInfo( s.Letter
                                                                         , s.ScoringType == 4
                                                                         , s.Tables ?? 0
                                                                         , Rounds.Count(r => r.Key.Section == s.Id)
                                                                         , Rounds[(s.Id, 1)][0].BoardsPerRound)
                                                   );

                initPlayedBoards();
                MarkChangedSettingsOnTimers();
                startPolling();
                return;
            }
            catch (Exception ex)
            {
                Logger.Exception(ex, $"BridgeMate: error opening/handling the file {fileInfo.FullName}");
                Debugger.Break();
                continue;
            }

        Logger.Info($"BridgeMate: file not found for date {lastDate} in path {path}");
        Close();
        initWatcher(path, Configuration.ReadBridgeMate);
    }

    public void Close()
    {
        stopPolling();

        if (watcher?.EnableRaisingEvents == true)
        {
            watcher.EnableRaisingEvents = false;
            Logger.Info("BridgeMate watcher disabled");
        }

        foreach (var timer in Configuration.BridgeTimers)
            timer.UpdateBMSettings(null);

        bwsFile = null;
        RoundStatus.Clear();

        if (bmClubNo >  -1)
        {
            Logger.Info("BridgeMate database closed");
            bmClubNo = -1;
        }
    }

    #region Private Methods
        private void initPlayedBoards()
        {
            if (Rounds       is null
            ||  Rounds.Count == 0)
                return;

            using var db = new BridgeMateContext(bwsFile);

            foreach (var row in Rounds.Values.SelectMany(r => r))
                row.BoardsPlayed = ( row.Nspair == row.SectionEntity.MissingPair
                                 ||  row.Ewpair == row.SectionEntity.MissingPair)
                                 ? row.BoardsPerRound
                                 : 0;

            var receivedData = db.ReceivedData
                                 .Where(r => r.Erased != true)
                                 .OrderBy(r => r.Id)
                                 .ToList();

            foreach (var row in receivedData)
            {
                Rounds[(row.Section, row.Round)][row.TableNo - 1].BoardsPlayed++;

                row.Processed4 = true;
            }

            db.SaveChanges();

            Execute.BeginOnUIThread(() =>
            {
                RoundStatus.ReplaceRange(Rounds.Select(rounds => new RoundStatus
                                                                 {
                                                                     Section         = (short)rounds.Key.Section
                                                                   , Round           = (short)rounds.Key.Round
                                                                   , Letter          = Sections[rounds.Key.Section].Letter
                                                                   , Done            = rounds.Value.All(g => g.Done)
                                                                   , BoardsRemaining = rounds.Value.Sum(g => g.BoardsRemaining)
                                                                 }));
            });
        }

        private void updatePlayedBoards()
        {
            if (Rounds       is null
            ||  Rounds.Count == 0)
                return;

            using var db = new BridgeMateContext(bwsFile);

            var unprocessedData = db.ReceivedData
                                    .Where(r => r.Processed4 != true)
                                    .ToList();

            foreach (var row in unprocessedData)
            {
                if (row.Erased == true)
                    Rounds[(row.Section, row.Round)][row.TableNo - 1].BoardsPlayed--;
                else
                    Rounds[(row.Section, row.Round)][row.TableNo - 1].BoardsPlayed++;

                row.Processed4 = true;
            }

            db.SaveChanges();

            Execute.BeginOnUIThread(() =>
            {
                foreach (var stat in RoundStatus)
                {
                    var round            = Rounds[(stat.Section, stat.Round)];
                    stat.Done            = round.All(g => g.Done);
                    stat.BoardsRemaining = round.Sum(g => g.BoardsRemaining);
                }

            });
        }

        private void MarkChangedSettingsOnTimers()
        {
            // Mark all timers with the new settings, so that the user can see the correct
            // values in the TimerSettings dialog.
            foreach (var item in Sections)
            {
                var section = item.Value;

                foreach (var timer in Configuration.GetRelatedTimers(section.Letter))
                    timer.UpdateBMSettings(section.Teams, section.Rounds, section.BoardsPerRound);
            }
        }

        #region Polling
            void initWatcher(string path, bool enableRaisingEvents = true)
            {
                if (File.Exists(path))
                {
                    // path is a full file path
                    watcher.Path = Path.GetDirectoryName(path);
                    watcher.Filters.Clear();
                    watcher.Filters.Add(Path.GetFileName(path));
                }
                else
                {
                    if (Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        // path is a directory path
                        watcher.Filters.Clear();
                        watcher.Filters.Add("*.bws");
                    }
                    else
                        watcher.Filter = path.FirstNonSharedDirectory(watcher.Path);

                    watcher.Path = path.FindDeepestExistingDirectory();
                }

                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents   = enableRaisingEvents;

                if (watcher.EnableRaisingEvents)
                    Logger.Info($"BridgeMate watcher enabled on path: {watcher.Path} ");
                else
                    Logger.Info($"BridgeMate watcher disabled");
            }

            private async Task handleFileEventAsync(FileSystemEventArgs ev)
            {
                try
                {
                    if (!ev.FullPath.StartsWith(Configuration.BridgeMatePath))
                    {
                        initWatcher(Configuration.BridgeMatePath, Configuration.ReadBridgeMate);
                        return;
                    }

                    var now = DateTime.UtcNow;

                    //Todo: Here we need to check if a new bws file has been created
                    switch (ev.ChangeType)
                    {
                        case WatcherChangeTypes.Changed:
                        case WatcherChangeTypes.Created:
                        case WatcherChangeTypes.Renamed:
                            Execute.BeginOnUIThread(() => CheckOrOpen(lastDate, bmClubNo));

                            break;

                        default:
                            Logger.Info($"BridgeMate: unhandled file event {ev.ChangeType} for file {ev.FullPath}");
                            break;
                    }
                }

                catch (Exception ex)
                {
                    Logger.Exception(ex, "BridgeMate.handleFileEventAsync");
                    MessageBox.Show($"{Lex.ErrorReadingBrideMateFolder}: " + ex.Message, Lex.Error);
                }
            }

            private bool IsPollingActive => _pollingTask != null
                                         && !_pollingTask.IsCompleted
                                         && !_pollingTask.IsCanceled
                                         && !_pollingTask.IsFaulted
                                         && !(_cts?.IsCancellationRequested ?? false);

            private void startPolling()
            {
                if (!IsPollingActive)
                {
                    Logger.Info("polling started");
                    _cts         = new();
                    _pollingTask = Task.Run(async () => await PollLoopAsync(_cts.Token));
                }
            }

            private void stopPolling()
            {
                if (IsPollingActive)
                {
                    Logger.Info("polling stopped");
                    _cts.Cancel();
                }
            }

            private async Task PollLoopAsync(CancellationToken token)
            {
                var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

                try
                {
                    while (await timer.WaitForNextTickAsync(token))
                    {
                        updatePlayedBoards();
                    }
                }

                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    Logger.Debug("Stop polling");
                }
            }
        #endregion
    #endregion
}
