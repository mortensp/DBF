using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.BridgeMateModel;
using DBF.DataModel;
using DBF.Helpers;
using Microsoft.DotNet.DesignTools.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DBF;

// As long as the right database fiel isn't found a FilerWatcher is used to monitor the folder for changes.
// Once the *.bws file is found a Timer is used to prope for changes
public class BridgeMate : PropertyChangedBase
{
    private const    string                      SearchExt = "bws";
    private          SerializedFileSystemWatcher watcher;
    private readonly Configuration               Configuration;
    private          string                      bwsFile;
    private          int                         bmClubNo  = -1;
    private          DateTime                    lastDate;

    #region Public Properties
        public BindableCollection<BMRound>        BMRounds    { get; private set; } = new();
        public BindableCollectionExt<RoundStatus> RoundStatus { get; private set; } = new();
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

    public void CheckOrOpen(DateTime? playingTime, int? clubNumber)
    {
        if (!Configuration.ReadBridgeMate
        ||  playingTime is not DateTime date
        ||  clubNumber  is not int clubNo
        ||  clubNo      <= 0)
            return;

        Logger.Info($"");
        Logger.Info($"CheckOrOpen called playingTime={playingTime} clubNumber={clubNumber}");

        lastDate = date;

        var path  = $"{Configuration.BridgeMatePath}{clubNo}\\";
        var after = DateTime.Now.AddDays(-14); //TODO: rettes ned til 14 dage;

        if (!Directory.Exists(path))
        {
            Logger.Info($"BridgeMate: Directory not found: {path}");
            MessageBox.Show($"Mappen: '{path}' findes ikke. Så oplysningerne fra terminalerne kan ikke indlæses", "Fejl");
        }
        else
            foreach (var found in Directory.GetFiles(path, $"*.{SearchExt}")
                                           .Where(f => File.GetLastWriteTime(f) >= after)
                                           .OrderByDescending(f => File.GetLastWriteTime(f))) //TODO: skal det være oprettelses datetime?
            {
                var file = Path.ChangeExtension(found, ".bws");

                try
                {
                    using var db = new BridgeMateContext(file);

                    foreach (var session in db.Sessions)
                        if (session.Date == date)
                        {
                            // Ingen events, mens vi indlæser row og åbner filen
                            stopPolling();
                            bwsFile  = file;
                            bmClubNo = clubNo;

                            if (session.Status != 2)
                            {
                                BMRounds = new(db.BMRounds.Include(b => b.SectionEntity));

                                updatePlayedBoards(true);

                                foreach (var section in db.Sections)
                                {
                                    var cntRounds = BMRounds.Where(r=>r.Section==section.Id && r.TableNo==1).Count();
                                    var cntBoards = BMRounds.First().BoardsPerRound;

                                    foreach (var timer in Configuration.GetRelatedTimers(section.Letter))
                                        //if (timer.Rounds != cntRounds
                                        //||  BMRounds.Any(r => r.Done == false && r.Section == section.Id))
                                            timer.UpdateBMSettings(section.ScoringType == 4, cntRounds, cntBoards);
                                }

                                startPolling();
                            }

                            //initWatcher(file);
                            return;
                        }
                }
                catch (Exception ex)
                {
                    Logger.Exception(ex, $"BridgeMate: error opening/handling the file {file}");
                    continue;
                }
            }

        // BridgeMate er fil ikke fundet
        Logger.Info($"BridgeMate: file not found for date {lastDate} in path {path}");
        Close();

        bmClubNo = clubNo;
        initWatcher(path, Configuration.ReadBridgeMate);
    }

    public void Close()
    {
        //var gem =bmClubNo;
        stopPolling();

        if (watcher?.EnableRaisingEvents == true)
        {
            watcher.EnableRaisingEvents = false;
            Logger.Info("BridgeMate watcher disabled");
        }

        foreach (var timer in Configuration.BridgeTimers)
            timer.UpdateBMSettings(null);

        bwsFile  = null;
        BMRounds = null;
        BMRounds?.Clear();
        RoundStatus.Clear();

        if (bmClubNo >  -1)
        {
            Logger.Info("BridgeMate database closed");
            bmClubNo = -1;
        }
    }

    #region Private Methods
        void initWatcher(string path, bool enableRaisingEvents = true)
        {
            if (File.Exists(path))
            {
                watcher.Path = Path.GetDirectoryName(path);
                watcher.Filters.Clear();
                watcher.Filters.Add(Path.GetFileName(path));
            }
            else
            {
                if (Directory.Exists(Path.GetDirectoryName(path)))
                {
                    watcher.Filters.Clear();
                    watcher.Filters.Add("*." + SearchExt);
                }
                else
                    watcher.Filter = path.FirstNonSharedDirectory(watcher.Path);

                watcher.Path = path.FindDeepestExistingDirectory();
            }

            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents   = enableRaisingEvents;

            if (watcher.EnableRaisingEvents)
                Logger.Info($"BridgeMate watcher enabled on path:{watcher.Path} ");
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

                //Todo: Her skal jeg tjekke om, der er lavet en ny bws fil 
                switch (ev.ChangeType)
                {
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Renamed:
                        Execute.BeginOnUIThread(() => CheckOrOpen(lastDate, bmClubNo));

                        break;

                    default:
                        Logger.Info($"BridgeMate: unhandled file event {ev.ChangeType} for file {ev.FullPath}");
                        Debug.WriteLine($"File {ev.ChangeType}: {ev.Name}");
                        break;
                }
            }

            catch (Exception ex)
            {
                Logger.Exception(ex, "BridgeMate.handleFileEventAsync");
                MessageBox.Show($"Fejl ved læsning af BridgeMate mappen: " + ex.Message, "Fejl");
            }
        }

        private void updatePlayedBoards(bool first = false)
        {
            if (BMRounds is null)
                return;

            using var db = new BridgeMateContext(bwsFile);

            // Count bords played for each roundStatus. We do this by looking at the ReceivedData, which contains a record for
            // each board played. We count the number of records for each roundStatus, and update the BoardsPlayed property of
            // the BMRounds accordingly.
            // Note that BMRounds has on entry per table per roundStatus. ie 6 entries per Group when having two tables
            if (first)
            {
                foreach (var row in BMRounds)
                    if (row.Nspair == row.SectionEntity.MissingPair
                    ||  row.Ewpair == row.SectionEntity.MissingPair)
                        row.BoardsPlayed = row.BoardsPerRound;
                    else
                        row.BoardsPlayed = 0;

                // build index once
                var roundIndex = BMRounds.ToDictionary(r => (r.Section,r.TableNo, r.Round));

                foreach (var row in db.ReceivedData.OrderBy(r => r.Id))
                {
                    row.Processed4 = true;

                    if (row.Erased != true
                    &&  roundIndex.TryGetValue((row.Section, row.TableNo, row.Round), out var round))
                        round.BoardsPlayed++;
                }
            }
            else
                foreach (var data in db.ReceivedData.Where(r => r.Processed4 != true))
                {
                    data.Processed4 = true;

                    var round = BMRounds.FirstOrDefault(r =>  r.TableNo == data.TableNo
                                                          &&  r.Round   == data.Round);

                    if (round != null)
                        if (data?.Erased == true)
                            round.BoardsPlayed--;
                        else
                            round.BoardsPlayed++;
                }

            db.SaveChanges();

            // Update the RoundStatus collection, which is used to display the status of each roundStatus in the UI. We group the
            // BMRounds by section and roundStatus, and create a RoundStatus object for each group. We then update the existing
            // RoundStatus objects or add new ones as needed.
            // Note that RoundStatus has one entry per Round and does not have a table property 
            short  section =-1;
            string letter  ="";

            foreach (var roundStatus in BMRounds
                                       .GroupBy(o => (o.Section, o.Round))
                                       .OrderBy(g => g.Key.Section)
                                       .ThenBy(g => g.Key.Round)
                                       .Select(g => new RoundStatus
                                                    {
                                                        Section       = (short)g.Key.Section
                                                      , Round         = (short)g.Key.Round
                                                      , Done          = g.Count(g => g.Done) == db.Tables.Count(t => t.Section == g.Key.Section)
                                                      , RemaingBoards = g.Sum  (g => g.BoardsRemaining)
                                                    }))

            {
                if (section != roundStatus.Section)
                {
                    letter  = db.Sections.FirstOrDefault(s => s.Id == roundStatus.Section)?.Letter;
                    section = roundStatus.Section;
                }

                roundStatus.Letter = letter;

                var existing = RoundStatus.FirstOrDefault(r =>  r.Section == roundStatus.Section
                                                            &&  r.Round   == roundStatus.Round);

                if (existing is null)
                    RoundStatus.Add(roundStatus);
                else
                {
                    existing.Done          = roundStatus.Done;
                    existing.RemaingBoards = roundStatus.RemaingBoards;
                }
            }
        }

        #region Polling
            private CancellationTokenSource _cts = new();
            private Task                    _pollingTask;

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
                        Logger.Debug("Polling....");
                        updatePlayedBoards();
                    }
                }

                catch (OperationCanceledException)
                {
                    Logger.Debug("Stop polling");
                    // Normal shutdown
                }
            }
        #endregion
    #endregion
}
