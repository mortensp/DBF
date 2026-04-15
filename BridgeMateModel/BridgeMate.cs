using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.BridgeMateModel;
using DBF.DataModel;
using DBF.Helpers;
//using Microsoft.DotNet.DesignTools.Protocol.Values;
namespace DBF;

// As long as the right database fiel isn't found a FilerWatcher is used to monitor the folder for changes.
// Once the *.bws file is found a Timer is used to prope for changes
public class BridgeMate : PropertyChangedBase
{
    // BSC might not be running when developing, so we need to look for the .bws file, which is
    // the main database file for BridgeMate. In production, we  want to look for .ldb files,
    // which are lock files created when the database is in use.
    #if DEBUG
    private const string SearchExt = "bws";
#else
    private const string SearchExt = "ldb";
#endif
    private          SerializedFileSystemWatcher            watcher;
    private readonly DispatcherTimer                        timer;
    private readonly ConcurrentDictionary<string, DateTime> lastFileEvent = new();
    private          ReceivedData                           last          = new();
    private          BridgeMateContext                      db;
    private readonly Configuration                          Configuration;
    private          string                                 bmFile;
    private          int                                    bmClubNo      = -1;
    private          DateTime                               lastDate;
    private          int                                    lastClub      = -1;

    #region Public Properties
        public Session                                     Session  { get; private set; }
        public BindableCollection<BridgeMateModel.Table> Tables      { get; private set; } = new();
        public BindableCollection<RoundData>             Rounds      { get; private set; } = new();
        public BindableCollection<ReceivedData>          Received    { get; private set; } = new();
        public BindableCollectionExt<RoundStatus>        RoundStatus { get; private set; } = new();
    #endregion

    #region Constructors
        public BridgeMate(Configuration _configuration)
        {
            Configuration = _configuration;
            //
            watcher              = new();
            watcher.UpdatedAsync+= handleFileEventAsync;
            initWatcher(Configuration.BridgeMatePath, false);
            //
            timer      = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
            timer.Tick+= Timer_Tick;
        }
    #endregion

    public void CheckOrOpen(DateTime? playintTime, int? clubNumber)
    {
        if (!Configuration.ReadBridgeMate
        ||  playintTime is not DateTime date
        ||  clubNumber  is not int clubNo
        ||  clubNo      <= 0)
            return;

 //if (date   == Session?.Date
        //&&  clubNo == bmClubNo
        //&&  bmFile is not null)
        //    return; // Allerede åbnet

        lastDate = date;
        lastClub = clubNo;

        var path  = $"{Configuration.BridgeMatePath}{clubNo}\\";
        var after = DateTime.Now.AddDays(-14); //TODO: rettes ned til 14 dage;

        if (!Directory.Exists(path))
            MessageBox.Show($"Mappen: '{path}' findes ikke. Så oplysningerne fra terminalerne kan ikke indlæses", "Fejl");
        else
            foreach (var found in Directory.GetFiles(path, $"*.{SearchExt}")
                                          .Where(f => File.GetLastWriteTime(f) >= after)
                                          .OrderByDescending(f => File.GetLastWriteTime(f))) //TODO: skal det være oprettelses datetime?
            {
                var file = found.Replace(".ldb", ".bws");
                db = new BridgeMateContext(file);
                try
                {
                    foreach (var session in db.Sessions)
                        if (session.Date == date)
                        {
                            if (file == bmFile)
                                return;

                            // Ingen events, mens vi indlæser data og åbner filen
                            watcher.EnableRaisingEvents = false;
                                bmFile   = file;
                                bmClubNo = clubNo;
                                lastClub = -1;      // undgå gentagne åbniner, hvis der er ændringer i mappen
                                Session  = session;

                                if (Session.Status != 2)
                                {
                                    Tables = new(db.Tables);
                                    Rounds = new(db.RoundData);
                                updatePlayedBoards();
                                timer.Start();
                                }

                            watcher.Path                = path;
                            watcher.Filter              = Path.GetFileName(file);
                            watcher.EnableRaisingEvents = Configuration.ReadBridgeMate;

                            return;
                        }
                }
                catch (Exception)
                {
                    continue;
                    //MessageBox.Show($"BridgeMate Serveren er ikke startet korrekt", "Fejl");
                }
            }

        // BridgeMate er fil ikke fundet
        Close();

        initWatcher(path, Configuration.ReadBridgeMate);
    }

    public void Close()
    {
        if (watcher is not null)
            watcher.EnableRaisingEvents = false;

        timer.Stop();
        bmFile   = null;
        bmClubNo = -1;
        Session  = null;
        db       = null;
        Received.Clear();
    }
    #region Private Methods
        void initWatcher(string path, bool enableRaisingEvents)
        {
            if (Directory.Exists(path))
            {
                watcher.Filters.Clear();
                watcher.Filters.Add("*." + SearchExt);
            }
            else
                watcher.Filter = path.FirstNonSharedDirectory(watcher.Path);

            watcher.Path                  = path.FindDeepestExistingDirectory();
            watcher.IncludeSubdirectories = false;
            watcher.EnableRaisingEvents   = enableRaisingEvents;
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

            switch (ev.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    CheckOrOpen(lastDate, lastClub);

                    break;

                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Renamed:
                    CheckOrOpen(lastDate, lastClub);
                    break;

                default:
                    Debug.WriteLine($"File {ev.ChangeType}: {ev.Name}");
                    break;
            }
        }

        catch (Exception ex)
        {
            MessageBox.Show($"Fejl ved læsning af BridgeMate mappen: " + ex.Message, "Fejl");
        }
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
            updatePlayedBoards();
        }

        private void updatePlayedBoards()
        {
        int cnt   = Received.Count;
        var table = Tables;

        foreach (var data in db.ReceivedData.Where(r =>  r.DateLog >  last.DateLog
                                                     ||  r.DateLog == last.DateLog
                                                     &&  r.TimeLog >  last.TimeLog)
                                            .OrderBy(r => r.TimeLog))
        {
            last = data;

                var round = Rounds.FirstOrDefault(r =>  r.TableNo == data.TableNo
                                                    &&  r.Round   == data.Round);

                if (round != null)
            if (data?.Erased == true)
            {
                Received.Remove(data);
                round.BoardsPlayed--;
            }
            else
            {
                Received.Add(data);
                round.BoardsPlayed++;
            }
                else
                    Debugger.Break();
        }

        if (Received.Count >  cnt)
                NotifyOfPropertyChange(nameof(Received));

            foreach (var round in Rounds
                  .GroupBy(o => (o.Section, o.Round))
                  .OrderBy(o => o.Key)
                                 .Select(g => new RoundStatus
                  {
                                                  Section       = g.Key.Section
                                                , Round         = g.Key.Round
                                                , Done          = g.Count(g => g.Done) == Tables.Count(t => t.Section == g.Key.Section)
                                                , RemaingBoards = g.Sum  (g => g.BoardsPerRound - g.BoardsPlayed)
                                              }))
            {
                var existing = RoundStatus.FirstOrDefault(r =>  r.Section == round.Section
                                                            &&  r.Round   == round.Round);

                if (existing is null)
                {
                    round.Letter = db.Sections.FirstOrDefault(s => s.Id == round.Section)?.Letter;
                    RoundStatus.Add(round);
                }
                else
    {
                    existing.Done          = round.Done;
                    existing.RemaingBoards = round.RemaingBoards;
                }
    }
}
    #endregion
}