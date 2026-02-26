using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using DBF.BridgeMateModel;
using DBF.DataModel;
//using Microsoft.DotNet.DesignTools.Protocol.Values;

namespace DBF
{
    public class BridgeMate : INotifyPropertyChanged
    {
        private          FileSystemWatcher                      watcher = new();
        private readonly DispatcherTimer                        timer;
        private readonly ConcurrentDictionary<string, DateTime> lastFileEvent = new();
        private          ReceivedData                           last          = new();
        private          BridgeMateContext                      db;
        private readonly Configuration                          configuration;
        private          string                                 bmFile;
        private          int                                    bmClubNo      = -1;
        private          DateTime                               lastDate;
        private          int                                    lastClub      = -1;

        #region Public Properties
            public Session                                     Session  { get; private set; }
            public ObservableCollection<BridgeMateModel.Table> Tables   { get; private set; } = new();
            public ObservableCollection<RoundData>             Rounds   { get; private set; } = new();
            public ObservableCollection<ReceivedData>          Received { get; private set; } = new();

            public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
            public BridgeMate(Configuration _configuration)
            {
                configuration = _configuration;
            //
            if (Directory.Exists(configuration.BridgeMatePath))
            {
                watcher = new FileSystemWatcher(configuration.BridgeMatePath);
                watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime;
                watcher.Filter = "*.bws";
                watcher.IncludeSubdirectories = true;
                watcher.Created += fileCreated;
            }
                //
                timer      = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
                timer.Tick+= Timer_Tick;
            }
        #endregion

        public void CheckOrOpen(DateTime date, int clubNo)
        {
            if (clubNo <  0)
                return;

            if (date   == Session?.Date
            &&  clubNo == bmClubNo
            &&  bmFile is not null)
                return; // Allerede åbnet

            lastDate = date;
            lastClub = clubNo;

            var path  = $"{configuration.BridgeMatePath}{clubNo}\\";
            var after = DateTime.Now.AddDays(-14); //TODO: rettes ned til 14 dage;

            if (!Directory.Exists(path))
                MessageBox.Show($"Mappen: '{path}' findes ikke. Så oplysningerne fra terminalerne kan ikke indlæses", "Fejl");
            else
                foreach (var file in Directory.GetFiles(path, "*.bws")
                                              .Where(f => File.GetLastWriteTime(f) >= after)
                                              .OrderByDescending(f => File.GetLastWriteTime(f))) //TODO: skal det være oprettelses datetime?
                {
                    db = new BridgeMateContext(file);
                    try
                    {
                        foreach (var session in db.Sessions)
                            if (session.Date == date)
                            {
                                if (file != bmFile)
                                {
                                    bmFile   = file;
                                    bmClubNo = clubNo;
                                    lastClub = -1;      // undgå gentagne åbniner, hvis der er ændringer i mappen
                                    Session  = session;
                                    db       = new BridgeMateContext(bmFile);

                                    if (Session.Status != 2)
                                    {
                                        timer.Start();
                                        Tables = new(db.Tables);
                                        Rounds = new(db.RoundData);
                                    }

                                    watcher.EnableRaisingEvents = false;
                                }

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

            watcher.EnableRaisingEvents = true;
        }

    
        public void Close()
        {
            watcher.EnableRaisingEvents = false;
            timer.Stop();
            bmFile   = null;
            bmClubNo = -1;
            Session  = null;
            db       = null;
            Received.Clear();
        }

        private void fileCreated(object sender, FileSystemEventArgs e)
        {
            // Debounce: Ignorer events for samme fil inden for 500 ms
            try
            {
                var now = DateTime.UtcNow;

                if (lastFileEvent.TryGetValue(e.FullPath, out DateTime last))
                    if ((now - last).TotalMilliseconds <  500)
                        return; // Ignorer duplikat
                    else
                        lastFileEvent[e.FullPath] = now;
                else
                    lastFileEvent.TryAdd(e.FullPath, now);

                if (e.ChangeType == WatcherChangeTypes.Created)
                    CheckOrOpen(lastDate, lastClub);
                else
                    Debug.WriteLine($"Unhandled update: {e.Name} - {e.ChangeType}");
            }
            catch (Exception)
            {
                MessageBox.Show($"Fejl ved læsning af BridgeMate mappen", "Fejl");
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            int cnt   = Received.Count;
            var table = Tables;

            foreach (var data in db.ReceivedData.Where(r =>  r.DateLog >  last.DateLog
                                                         ||  r.DateLog == last.DateLog
                                                         &&  r.TimeLog >  last.TimeLog)
                                                .OrderBy(r => r.TimeLog))
            {
                last = data;

                var round = Rounds.First(r => r.Table == data.Table && r.Round == data.Round);

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
            }

            if (Received.Count >  cnt)
                OnPropertyChanged(nameof(Received));

            var rnds = Rounds
                        .GroupBy(o => (o.Section, o.Round))
                        .OrderBy(o => o.Key)
                        .Select(g => new
                        {
                            Section = g.Key.Section,
                            Round = g.Key.Round,
                            Done = g.Count(g => g.Done) == Tables.Count(t => t.Section == g.Key.Section)
                        })
                        .ToList();
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
