using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Serialization;

using Caliburn.Micro;

using DBF.Converters;
using DBF.DataModel;
using DBF.Helpers;
using DBF.UserControls;
using DBF.Views;

using Syncfusion.Data.Extensions;

namespace DBF.ViewModels
{
    public class ControlViewModel : Screen, IDisposable
    {
        private readonly        IWindowManager                  windowManager;
        private                 Club                            selectedClub;
        private                 UserControl                     startListControl = new StartListControl();
        private                 TimersPanel                     timersPanel      = new (Visibility.Collapsed);
        private                 UserControl                     resultsControl   = new ResultsControl();
        private                 MainClub                        selectedMainClub;
        private                 PlayingTime                     playingTime;
        private                 List<Tournament>                tournaments;
        private                 JsonSerializerOptions           JsonOptions      = new() { Converters = {new DecimalCommaConverter()}};
        private                 Encoding                        iso_8859_1       = Encoding.GetEncoding("iso-8859-1");
        private                 BindableCollection<PlayingTime> playingdates     = [];
        private                 int                             sectionNo;
        private                 bool                            showAsOneGroup   = true;
        private static readonly TimeSpan                        threshold        = new TimeSpan(0, 0,  10);
        private                 SerializedFileSystemWatcher     watcher;

        private bool disposed;

        #region Constructors
            public ControlViewModel(IWindowManager windowManager, Configuration configuration, BridgeMate bridgeMate)
            {
                //CurrentView = timersPanel;
                BridgeMate = bridgeMate;

                if (BridgeMate?.RoundStatus is not null)
                    BridgeMate.RoundStatus.ItemChanged += roundStatusItemChanged;

                Configuration                       = configuration;
                this.windowManager                  = windowManager;
                Thread.CurrentThread.CurrentCulture = Global.DkCulture;

                watcher                    = new();
                watcher.EventGroupingDelay = TimeSpan.FromMilliseconds(7000);
                watcher.UpdatedAsync      += handleFileEventAsync;
                //
                Pairs.CollectionChanged+= pairsCollectionChanged;
                Teams.CollectionChanged+= teamsCollectionChanged;
                //
                initWatcher();
                loadMainClubs();
            }
        #endregion

        #region Public Properties
            public Configuration            Configuration            { get; set; }
            public BridgeMate               BridgeMate               { get; set; }
            public bool                     HideTournamentSummery    { get; set; }
            public bool                     ImpsPair                 { get; set; }
            public bool                     HideHacGrp               { get; set; } = true;
            public DateTime                 Date                     { get; set; }
            public List<GroupSection>       GroupSections            { get; set; }
            public BindableCollection<Pair> Pairs                    { get; set; } = [];
            public BindableCollection<Team> Teams                    { get; set; } = [];
            public Visibility               ShowAsOneGroupVisibility { get; set; } = Visibility.Collapsed;
            public UserControl              CurrentView              { get; private set; }
            public bool                     BC3Available             => SelectedPlayingTime != null;

            public int SectionNo
            {
                get => sectionNo;
                set
                {
                    if (Set(ref sectionNo, value))
                        HideTournamentSummery = SectionNo <  2;
                }
            }

            public string ErrorMessage
            {
                get => field;
                set
                {
                    if (Set(ref field, value)
                    &&  value != null)
                        Logger.Info(value);
                }
            }

            public bool ShowAsOneGroup
            {
                get => showAsOneGroup;
                set
                {
                    var old = showAsOneGroup;

                    if (Set(ref showAsOneGroup, value))
                        if (value == true)
                            foreach (var pair in Pairs)
                            {
                                pair.SubGroup  = "";
                                pair.Placering = pair.SectionRank;
                            }
                        else
                            initSubgroups();
                }
            }

            #region Main Club(s)
                public ObservableCollection<MainClub> MainClubs { get; set; } = [];

                public MainClub SelectedMainClub
                {
                    get => selectedMainClub;
                    set
                    {
                        ErrorMessage = "";

                        if (Set(ref selectedMainClub, value))
                        {
                            //BridgeMate.Close();
                            if (value == null)
                            {
                                PlayingTimes.Clear();
                                SelectedPlayingTime = null;
                            }
                            else
                            {
                                Clubs = SelectedMainClub.Clubs?.OrderBy(c => c.Name)
                                                               .ToObservableCollection();

                                Logger.Info($"SectedMainClub changed: {value}");

                                SelectedClub = null; // nødvendigt, da club og SelectedClub kun sammenlignes på feltet Id, dvs. kan væe ens.
                                SelectedClub = Clubs?.FirstOrDefault();
                            }
                        }
                    }
                }
            #endregion

            #region SubClub(s)
                public ObservableCollection<Club> Clubs { get; set; } = [];

                public Club SelectedClub
                {
                    get => selectedClub;
                    set
                    {
                        ErrorMessage = "";

                        if (Set(ref selectedClub, value))
                        {
                            //BridgeMate.Close();
                            Logger.Debug($"SectedClub changed: {value?.ToString() ?? "Null"}");

                            if (value is null)
                            {
                                PlayingTimes.Clear();
                                //SelectedPlayingTime = null;
                            }
                            else
                                fetchPlayingTimes();
                        }
                    }
                }
            #endregion

            #region PlayingTime(s)
                public BindableCollection<PlayingTime> PlayingTimes
                {
                    get => playingdates;
                    set
                    {
                        var before = DateTime.Now.Date.AddDays(1);
                        var after  = DateTime.Now.Date.AddDays(-6);

                        if (Set(ref playingdates, value))
                            SelectedPlayingTime = PlayingTimes.Where(pt => pt.Date <= before && pt.Date >  after).FirstOrDefault()
                                               ?? PlayingTimes.Where(pt => pt.Date >  DateTime.Now.Date).LastOrDefault()
                                               ?? PlayingTimes.LastOrDefault();
                    }
                }

                public PlayingTime SelectedPlayingTime
                {
                    get => playingTime;
                    set
                    {
                        if (Set(ref playingTime, value))
                        {
                            BridgeMate.Close();

                            if (value is not null)
                            {
                                fetchPlayingTime();

#if DEBUG
                                if (tournaments.Count >  0
                                &&  Configuration.ReadBridgeMate)
                                {
                                    BridgeMate.CheckOrOpen(SelectedPlayingTime.Date, SelectedMainClub.No);

                                    //if (Configuration.ReadBridgeMate)
                                    if (File.Exists(Configuration.StatePath))
                                        Configuration.DeleteState();
                                    else
                                    {
                                        // Liste af RoundStatus entries som er Done og som har det højeste Round-nummer pr. Section
                                        var highestDonePerSection = BridgeMate.RoundStatus
                                                                              .Where(r => r.Done )
                                                                              .GroupBy(r => r.Section)
                                                                              .SelectMany(g =>
                                                                                          {
                                                                                              var maxRound = g.Max(x => x.Round);
                                                                                              return g.Where(x => x.Round == maxRound);
                                                                                          })
                                                                              .ToList();

                                        foreach (var timer in Configuration.BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                                            timer.Reset(false);

                                        foreach (var rs in highestDonePerSection)
                                            foreach (var timer in Configuration.GetRelatedTimers(rs, threshold))
                                            {
                                                //if (timer.Rounds >  rs.Round)
                                                    timer.SetRound(rs.Round + 1);
                                            }
                                    }
                                }

#endif
                            }
                        }
                    }
                }
            #endregion
        #endregion

        #region Public Methods
            public void Test()     => Debugger.Break();

            public void AddTimer() => Configuration.AddTimer();

            #region Public Projector Methods
                public async Task ShowStartList()
                {
                    if (CurrentView is not StartListControl)
                        CurrentView = startListControl;

                    await showProjector().ConfigureAwait(false);
                }

                public async Task ShowBridgeTimers()
                {
                    if (CurrentView is not TimersPanel)
                        CurrentView = timersPanel;

                    await showProjector().ConfigureAwait(false);
                }

                public async Task ShowResults()
                {
                    if (CurrentView is not ResultsControl)
                        CurrentView = resultsControl;

                    await showProjector().ConfigureAwait(false);
                }

                public void CloseProjector()
                {
                    var projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    if (projectorView is not null)
                    {
                        CurrentView = null;
                        projectorView.Close();
                    }
                }
            #endregion

            public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (Configuration.TimersActive
                    &&  Window.GetWindow(CurrentView)?.GetType().Name != "ProjectorView")
                    {
                        // show custom dialog with three choices
                        var owner = Window.GetWindow(CurrentView) ?? Application.Current.MainWindow;
                        var dlg   = new DBF.Views.ConfirmCloseDialog("Hvis du lukker vinduet, så nulstilles alle aktive ure. Hvad vil du gøre?");
                        dlg.Owner = owner;
                        dlg.ShowDialog();

                        switch (dlg.Choice)
                        {
                            case ConfirmCloseChoice.Close:
                                Configuration.DeleteState();
                                return await Task.FromResult(true);

                            case ConfirmCloseChoice.Cancel:
                                return await Task.FromResult(false);

                            case ConfirmCloseChoice.SaveState:
                                Configuration.SaveState();
                                return await Task.FromResult(true);
                        }
                    }
                    else
                    {
                        Configuration.DeleteState();
                        return await Task.FromResult(true);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fejl ved lukning af ControlViewModel: {ex.Message}");
                }

                return await Task.FromResult(true);
            }
        #endregion

        #region Private Method
            void initWatcher()
            {
                watcher.Path = Configuration.HomepagePath.FindDeepestExistingDirectory();

                if (watcher.Path + "\\" == Configuration.HomepagePath)
                {
                    watcher.Filters.Clear();
                    watcher.LikeFilters.Add(@"Resultater_????*");
                    watcher.Filters.Add("Main.XML");
                    watcher.IncludeSubdirectories = Configuration.ReadBC3;
                }
                else
                {
                    watcher.Filter                = Configuration.HomepagePath.FirstNonSharedDirectory(watcher.Path);
                    watcher.IncludeSubdirectories = true;
                }

                Logger.Info($"Files Watcher set up on path: {watcher.Path}");
            }

            private void showMessageAFewSeconds(string msg)
            {
                Logger.Info(msg);
                ErrorMessage = msg;

                // Clear the error message after 10 seconds without blocking the UI thread
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000);
                    await Execute.OnUIThreadAsync(() =>
                    {
                        if (ErrorMessage == msg)
                            ErrorMessage =  string.Empty;
                        return Task.CompletedTask;
                    });
                });
            }

            private void fetchPlayingTimes()
            {
                try
                {
                    //ObservableCollection<PlayingTime> playingtimes = [];
                    var mainTournaments = SelectedClub is null
                                        ? SelectedMainClub.Clubs.SelectMany(club => club.MainTournaments)
                                        : SelectedClub.MainTournaments;

                    //foreach (var mt in mainTournaments)
                    //    foreach (var pt in mt.PlayingTimes)
                    //    {
                    //        pt.MainTournament = mt;
                    //        playingtimes.Add(pt);
                    //    }

                    //PlayingTimes = new BindableCollection<PlayingTime>(playingtimes.OrderByDescending(s => s.Date));
                    PlayingTimes = new BindableCollection<PlayingTime>(
                                 mainTournaments.SelectMany(mt => mt.PlayingTimes, (mt, pt) =>
                                 {
                                 pt.MainTournament = mt;
                                 return pt;
                                 })
                                                .OrderByDescending(pt => pt.Date));
                }

                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    PlayingTimes.Clear();
                }
            }

            /// <summary>
            /// Fetch XML data for the chosen playing day and time
            /// </summary>
            private void fetchPlayingTime(bool newSession = true)
            {
                ShowAsOneGroup                 = true;
                HideHacGrp                     = true;
                ShowAsOneGroupVisibility       = Visibility.Collapsed;
                ErrorMessage                   = "";
                BindableCollection<Pair> pairs = [];
                BindableCollection<Team> teams = [];
                Pairs.Clear();
                Teams.Clear();

                //bool RoundCompleted   = true;
                try
                {
                    Logger.Info($"Loading playingtimer: {playingTime}");
                    Configuration.StartDate = playingTime.Date;
                    tournaments             = getTournaments(playingTime);

                    if (tournaments.Count == 0)
                        return;

                    GroupSections = getGroupSections(playingTime, tournaments);
                    Date          = playingTime.Date;

                    for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                    {
                        var grp = GroupSections[grpNo];

                        //if (!grp.Completed)
                        //    RoundCompleted = false;
                        if (grp.Tournament.TournamentType.Text == "Parturnering")
                            buildpairs(pairs, grpNo, grp);
                        else
                            buildTeams(teams, grpNo, grp);
                    }
                }
                catch (Exception)
                {
                    ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                }

                // Assign EntryNo for sorting in UI
                int i =0;

                foreach (var pair in pairs.OrderBy(p => p.Group).ThenBy(p => p.SubGroup).ThenBy(p => p.PairNo))
                    pair.EntryNo = i++;

                foreach (var team in teams.OrderBy(p => p.Group).ThenBy(t => t.TeamNo))
                    team.EntryNo = i++;

                initSubgroups(pairs);
                Pairs                       = pairs;
                Teams                       = teams;
                watcher.EnableRaisingEvents = Configuration.ReadBC3;
                Logger.Info($"Loaded  playingtimer: {playingTime}");

                // Restore Taskbar Icon.
                //Execute.OnUIThread(() =>
                //                   {
                //                       Application.Current.MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/DBF_Tools.ico", UriKind.Absolute));
                //                   });
            }

            private void buildTeams(BindableCollection<Team> teams, int grpNo, GroupSection grp)
            {
                foreach (var team in grp.Rounds[0].Startlist.Teams)
                {
                    team.Group = grp.Tournament.Title;
                    teams.Add(team);
                }

                // Merge the four lists, ie. start, results, HAC and Butler
                var rnd = grp.Rounds[^1];

                if (rnd is not null && rnd.RoundCompleted)
                    foreach (var team in teams.Where(t => t.Group == grp.Tournament.Title))
                    {
                        var sta = rnd.Startlist.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);

                        if (sta is not null)
                        {
                            team.Merge(sta);
                            var res = rnd?.Resultlist.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);
                            var hac = rnd?.HACResult?.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);
                            var but = rnd?.ButlerResult.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);
                            var oth = rnd?.Resultlist.Teams.FirstOrDefault(t => t.TeamNo == res.OpponentTeamNo);
                            //
                            team.Merge(res);
                            team.Merge(hac);
                            team.Merge(but);
                            team.TotalKP = team.KP ?? 0;
                        }
                    }
                else
                    ErrorMessage = "Den aktuelle sektion er endnu ikke afsluttet eller er ikke sendt til hjemmesiden!!";

                // Add KP from earlier sections
                foreach (var sectionFile in tournaments[grpNo].SectionFiles.Where(f => f.No <  grp.SectionNo))
                {
                    var earlierSection = getGroupSection(sectionFile.FileName, grp.Tournament);

                    rnd = earlierSection?.Rounds[^1];

                    if (rnd is not null && rnd.RoundCompleted)
                        foreach (var team in teams.Where(t => t.Group == grp.Tournament.Title))
                        {
                            var res = rnd.Resultlist.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);

                            if (res is not null)
                                team.TotalKP += res.KP ?? 0;
                        }
                    else
                        if (earlierSection is null)
                            ErrorMessage = $"En tidligere runde er endnu ikke afsluttet eller er ikke sendt til hjemmesiden!";
                        else
                            ErrorMessage = $"Runden d. {earlierSection.DateStr} er endnu ikke afsluttet eller er ikke sendt til hjemmesiden!";
                }

                // Setup TournamentRank Rank by Total KP
                var totalRank = 1;

                foreach (var team in teams.Where(t => t.Group == grp.Tournament.Title).OrderByDescending(t => t.TotalKP))
                    team.TournamentRank = totalRank++;
            }

            private void buildpairs(BindableCollection<Pair> pairs, int grpNo, GroupSection grp)
            {
                bool InterWovenHowell =false;
                bool Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
                ImpsPair              = grp.Tournament.TournamentPairCalcType == "3";

                if (grp.Resultlist is not null)
                {
                    InterWovenHowell = grp.Tournament.MovementPlan?.Contains("Indvævet Howell") ?? false;

                    foreach (var pair in grp.Resultlist.Pairs)
                    {
                        pair.GroupNo = grpNo;
                        pair.Group   = grp.Tournament.Title;

                        pairs.Add(pair);
                    }

                    if (Mitchell)
                    {
                        HideHacGrp = false;
                        var hacGrp = 1;

                        foreach (var pair in pairs.Where(p => p.Direction == "1").OrderBy(p => p.HACRankSection))
                            pair.HACRankSectionPart = hacGrp++;

                        hacGrp = 1;

                        foreach (var pair in pairs.Where(p => p.Direction == "2").OrderBy(p => p.HACRankSection))
                            pair.HACRankSectionPart = hacGrp++;
                    }
                    else
                        if (InterWovenHowell)
                        {
                            HideHacGrp       = false;
                            var subGroupSize = pairs.Count >> 1;
                            var hacGrp       = 1;

                            foreach (var pair in pairs.Take(subGroupSize).OrderBy(p => p.HACRankSection))
                                pair.HACRankSectionPart = hacGrp++;

                            hacGrp = 1;

                            foreach (var pair in pairs.Skip(subGroupSize).OrderBy(p => p.HACRankSection))
                                pair.HACRankSectionPart = hacGrp++;
                        }
                }

                if (grp.Startlist is not null)
                    foreach (var pair in grp.Startlist.Pairs)
                    {
                        var res = pairs.FirstOrDefault(p => p.Group == grp.Tournament.Title && p.PairNo == pair.PairNo);

                        if (res is null)
                        {
                            pair.GroupNo = grpNo;
                            pair.Group   = grp.Tournament.Title;
                            pairs.Add(pair);
                        }
                        else
                            res.StartPos = pair.StartPos;
                    }

                if (InterWovenHowell)
                {
                    ShowAsOneGroup           = false;
                    ShowAsOneGroupVisibility = Visibility.Visible;
                }
            }

            #region Load and Reload MainClub(s)
                private void loadMainClubs()
                {
                    if (!Configuration.ReadBC3)
                        return;

                    if (string.IsNullOrWhiteSpace(Configuration.HomepagePath)
                    || !Directory.Exists(Configuration.HomepagePath))
                    {
                        showMessageAFewSeconds($"Mappen: '{Configuration.HomepagePath}' findes ikke");
                        watcher.EnableRaisingEvents = Configuration.ReadBC3;
                        return;
                    }

                    Logger.Info($"Loading all Main.xml files in {Configuration.HomepagePath}");

                    foreach (var path in Directory.GetDirectories(Configuration.HomepagePath)
                                                  .Select(dir => Path.GetFileName(dir))
                                                  .Where(name => name.StartsWith("Resultater_", StringComparison.OrdinalIgnoreCase)))
                    {
                        var mainClub = loadMainClub(path);

                        if (mainClub is not null
                        && !MainClubs.Any(m => m.No == mainClub.No))
                            MainClubs.Add(mainClub);
                    }

                    if (MainClubs.Count == 0)
                    {
                        showMessageAFewSeconds($"Kan ikke finde Startliste i mappen: {Configuration.HomepagePath}");
                        return;
                    }

                    MainClubs = MainClubs.OrderBy(mc => mc.Name).ToObservableCollection();

                    if (selectedMainClub is null)
                    {
#if TEST
                        SelectedMainClub = MainClubs.FirstOrDefault(c => c.No == 9999) ?? MainClubs.First();
                        //SelectedPlayingTime = PlayingTimes.FirstOrDefault(p => p.DateStr.StartsWith("02-03-2026"));
#else
                        SelectedMainClub = MainClubs.FirstOrDefault(c => c.No != 9999) ?? MainClubs.First();
#endif
                    }
                }

                private MainClub loadMainClub(string path)
                {
                    if (path.Contains('\\') || path.Contains('/'))
                        path = path.GetLeafDirectoryName();
                    else
                        path = Path.GetFileName(path);

                    if (int.TryParse(path.Substring(11), out int no))
                        return loadMainClub(no);

                    return null;
                }

                private MainClub loadMainClub(int no)
                {
                    var path     = Configuration.HomepagePath + @$"Resultater_{no}\";
                    var filename = path + @"Main.xml";

                    try
                    {
                        var mainclub = deserialize<MainClub>(filename);

                        if (mainclub?.Clubs is null)
                            return null;

                        mainclub.Path = path;
                        mainclub.No   = no;

                        return mainclub;
                    }

                    catch (Exception)
                    {
                        ErrorMessage = $"Fejl ved læsning af Main.xml";
                        return null;
                    }
                }

                private void reloadSelectedClub(string mainPath)
                {
                    try
                    {
                        var mainNew  = loadMainClub(mainPath);
                        var mainClub = MainClubs.FirstOrDefault(m=>m.No==mainNew.No);

                        if (mainClub is null)
                        {
                            Logger.Info($"New mainclub read: {mainNew.Name}");
                            // a new main club
                            Execute.OnUIThread(() => MainClubs.Add(mainNew));
                            return;
                        }

                        if (mainNew?.Clubs is not null)
                            foreach (var clubNew in mainNew.Clubs)
                            {
                                var clubOld = mainClub.Clubs.FirstOrDefault(c => c.Id == clubNew.Id);

                                if (clubOld is null)
                                {
                                    Logger.Info($"New subclub read: {clubNew.Name}");

                                    // a new subClub
                                    if (mainClub.Clubs.Count == 0)
                                        Execute.OnUIThread(() =>
                                        {
                                            mainClub.Clubs.Add(clubNew);
                                            if (mainClub == SelectedMainClub)
                                                Clubs.Add(clubNew);
                                        });
                                    else
                                        for (var i = 0; i <  mainClub.Clubs.Count; i++)
                                        {
                                            // Ordered insert
                                            if (string.Compare(mainClub.Clubs[i].Name, clubNew.Name, StringComparison.CurrentCulture) <= 0)
                                            {
                                                Execute.OnUIThread(() => mainClub.Clubs.Insert(i, clubNew));

                                                if (mainClub == SelectedMainClub)
                                                    Clubs.Insert(i, clubNew);

                                                break;
                                            }
                                        }
                                }
                                else
                                    if (mainNew.No == mainClub.No
                                    &&  clubNew.Id == clubOld.Id)
                                    {
                                        Logger.Info($"Loading new tournaments for: {clubNew.Name}");

                                        foreach (var mt in clubNew.MainTournaments)
                                        {
                                            var mtOld =clubOld.MainTournaments.FirstOrDefault(m=>m.Name==mt.Name && m.Id==mt.Id);

                                            if (mtOld is null)
                                            {
                                                Execute.OnUIThread(() =>
                                                {
                                                    clubOld.MainTournaments.Add(mt);
                                                    foreach (var pt in mt.PlayingTimes)
                                                        addToPlayingTimes(mt, pt);
                                                });

                                                continue;
                                            }

                                            foreach (var playingTimeNew in mt.PlayingTimes)
                                            {
                                                var playingTimeOld = mtOld.PlayingTimes.FirstOrDefault(pt => pt.Date == playingTimeNew.Date);

                                                if (playingTimeOld is null)
                                                    Execute.OnUIThread(() => addToPlayingTimes(mt, playingTimeNew));
                                                else
                                                    if (playingTimeOld.Date == playingTimeNew.Date)
                                                        foreach (var fileNew in playingTimeNew.TournamentFiles)
                                                        {
                                                            var fileOld = playingTimeOld.TournamentFiles.FirstOrDefault(o => o.GroupName== fileNew.GroupName && o.Id==fileNew.Id);

                                                            if (fileOld is null)
                                                            {
                                                                Execute.OnUIThread(() =>
                                                                {
                                                                    playingTimeOld.TournamentFiles.Add(fileNew);
                                                                    PlayingTimes.Add(playingTimeNew);
                                                                    PlayingTimes        = new BindableCollection<PlayingTime>(PlayingTimes.OrderByDescending(s => s.Date));
                                                                    SelectedPlayingTime = null;
                                                                    SelectedPlayingTime = playingTimeOld;
                                                                });

                                                                return;
                                                            }
                                                            else
                                                                Execute.OnUIThread(() => { fileOld.Merge(fileNew); });
                                                        }
                                            }

                                            if (SelectedClub is null)
                                                Execute.OnUIThread(() => SelectedClub = mainClub.Clubs?.FirstOrDefault());
                                        }
                                    }
                            }
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = "Fejl ved læsning af Main.xml";
                        Logger.Exception(ex, ErrorMessage);
                    }
                }

                private void addToPlayingTimes(MainTournament mt, PlayingTime pt)
                {
                    pt.MainTournament = mt;

                    var cnt =PlayingTimes.Count;

                    for (var i = 0; i <  cnt; i++)
                    {
                        if (PlayingTimes[i].Date <  pt.Date)
                        {
                            Execute.OnUIThread(() => PlayingTimes.Insert(i, pt));
                            return; // break;
                        }
                    }

                    Execute.OnUIThread(() => PlayingTimes.Add(pt));
                }
            #endregion

            private void initSubgroups(BindableCollection<Pair> pairs = null)
            {
                pairs ??= Pairs;

                for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                {
                    var grp = GroupSections[grpNo];

                    if (grp.Tournament.TournamentType.Text == "Parturnering"
                    &&  grp.Resultlist                     is not null)
                    {
                        var InterwovenHowell = grp.Tournament.MovementPlan?.Contains("Indvævet Howell")??false;
                        var Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
                        var subGroupSize     = grp.Resultlist.Pairs.Count >> 1;
                        var rankA            = 1;
                        var rankB            = 1;

                        foreach (var pair in pairs.Where(p => p.GroupNo == grpNo).OrderBy(p => p.SectionRank))
                        {
                            pair.Placering = pair.Rank;
                            pair.Group     = grp.Tournament.Title;

                            if (InterwovenHowell)
                            {
                                pair.SubGroup  = pair.PairNo <= subGroupSize ? "1. halvdel" : "2. halvdel";
                                pair.Placering = pair.PairNo <= subGroupSize ? rankA++ : rankB++;
                            }
                            else
                                if (Mitchell)
                                {
                                    pair.SubGroup  = pair.Direction == "2" ? "ØV" : "NS";
                                    pair.Placering = pair.Rank;
                                }
                                else
                                {
                                    pair.SubGroup  = "";
                                    pair.Placering = pair.Rank;
                                }
                        }
                    }
                }
            }

            #region Get XML data
                private List<Tournament> getTournaments(PlayingTime pt)
                {
                    if (watcher is not null)
                        foreach (var pathName in watcher.Filters.Where(f => f.StartsWith("MT") || f.StartsWith("GT")).ToList())
                            watcher.Filters.Remove(pathName);

                    List<Tournament> tournaments = new();

                    if (pt is null || pt.TournamentFiles is null)
                        ErrorMessage = $"BC3 data er ikke sendt til hjemmesiden";
                    else
                        foreach (var tournamentFile in pt.TournamentFiles)
                        {
                            if (string.IsNullOrEmpty(tournamentFile.FileName))
                            {
                                if (ErrorMessage.StartsWith("BC3 data for"))
                                    ErrorMessage = $"BC3 data er ikke sendt til hjemmesiden";
                                else
                                    ErrorMessage = $"BC3 data for '{tournamentFile.GroupName}' er ikke sendt til hjemmesiden";

                                continue;
                            }

                            var path       = SelectedMainClub.Path + tournamentFile.FileName;
                            var tournament = deserialize<Tournament>(path);

                            watcher.Filters.Add(tournamentFile.FileName);

                            if (tournament is null)
                                if (string.IsNullOrEmpty(ErrorMessage))
                                    ErrorMessage = $"BC3 data for '{tournamentFile.GroupName}' er ikke sendt til hjemmesiden";
                                else
                                    ErrorMessage = $"BC3 data er ikke sendt til hjemmesiden";
                            else
                            {
                                tournament.SectionNo = tournamentFile.Section?.SectionNo ?? 1;
                                tournaments.Add(tournament);
                            }
                        }

                    return tournaments;
                }

                private List<GroupSection> getGroupSections(PlayingTime pt, List<Tournament> tournaments)
                {
                    List<GroupSection> sections = new();
                    SectionNo                   = 1;

                    foreach (var pathName in watcher.Filters.Where(f => f.StartsWith("GT")).ToList())
                        watcher.Filters.Remove(pathName);

                    foreach (var tur in tournaments)
                    {
                        var path    = SelectedMainClub.Path + tur.SectionFile.FileName;
                        var section = deserialize<GroupSection>(path);

                        watcher.Filters.Add(tur.SectionFile.FileName);

                        if (section != null)
                        {
                            section.Tournament = tur;
                            sections.Add(section);

                            if (tur.SectionNo >  SectionNo)
                                SectionNo = tur.SectionNo;
                        }
                    }

                    return sections;
                }

                private GroupSection getGroupSection(string fileName, Tournament tournament)
                {
                    var path    = SelectedMainClub.Path + fileName;
                    var section = deserialize<GroupSection>(path);

                    if (section != null)
                        section.Tournament = tournament;

                    return section;
                }

                private T deserialize<T>(string fullPath) where T : new()
                {
                    try
                    {
                        if (!File.Exists(fullPath))
                            return default;

                        if (Path.GetExtension(fullPath).ToLowerInvariant() == ".json")
                        {
                            //string json = File.ReadAllText(fullPath, iso_8859_1);
                            string json = readAllTextWithRetry(fullPath, iso_8859_1);
                            return JsonSerializer.Deserialize<T>(json, JsonOptions);
                        }
                        else // XML
                        {
                            // Hvis filen ikke findes, returner null
                            //string xml = File.ReadAllText(fullPath, iso_8859_1);
                            string xml = readAllTextWithRetry(fullPath, iso_8859_1);

                            // Erstat Fjern tag værdier, som kun består af blanke og - tegn
                            xml = Regex.Replace(xml, @">(-|\s)+<", "><");

                            // Erstat kun komma med punkt i decimaltal (fx 123,45 -> 123.45) - erstatter alle komaer mellem tal
                            xml = Regex.Replace(xml, @"(?<=\d),(?=\d)", ".");

                            // Remove empty tags
                            xml = Regex.Replace(xml, @"<(\w+)(\s[^>]*)?>\s*</\1>", string.Empty); //<TagName></TagName>

                            //xml2 = Regex.Replace(xml, @"<\w+\s*(/>|/>\s*</\1*s>)", string.Empty); //<TagName/>
                            xml = Regex.Replace(xml, @"<[A-Za-z_][A-Za-z0-9_.:-]*\s*/>", string.Empty); // removes self-closing tags like <Tag/>

                            var       serializer = new XmlSerializer(typeof(T));
                            using var reader     = new StringReader( xml);
                            return (T)serializer.Deserialize(reader);
                        }
                    }
                    catch (Exception)
                    {
                        Logger.Info($"Fejl ved deserialisering af fil: {fullPath}");
                        ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                        return default;
                    }
                }

                // Tilføj denne private helper-metode i samme klasse (f.eks. nederst i filen)
                private string readAllTextWithRetry(string path, Encoding encoding, int maxAttempts = 10, int initialDelayMs = 100)
                {
                    var delay = initialDelayMs;

                    for (int attempt = 1; attempt <= maxAttempts; attempt++)
                        try
                        {
                            // Åbn med ReadWrite sharing så vi kan læse selvom anden proces skriver (hvis den tillader deling).
                            using var fs = new FileStream(  path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            using var sr = new StreamReader(fs,   encoding);
                            return sr.ReadToEnd();
                        }

                        catch (IOException) when (attempt <  maxAttempts)
                        {
                            Thread.Sleep(delay);
                            delay = Math.Min(1000, delay * 2); // eksponentiel backoff, cap ved 1s
                        }

                        catch (UnauthorizedAccessException) when (attempt <  maxAttempts)
                        {
                            Thread.Sleep(delay);
                            delay = Math.Min(1000, delay * 2);
                        }

                    // Sidste forsøg (lader exception boble op hvis det fejler)
                    using var fsFinal = new FileStream(  path,    FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var srFinal = new StreamReader(fsFinal, encoding);
                    return srFinal.ReadToEnd();
                }
            #endregion

            private async Task showProjector()
            {
                var                    projectorScreen = WpfScreenHelper.Screen.AllScreens
                                                                               .Where(s => !s.Primary)
                                                                               .OrderByDescending(s => s.Bounds.Width * s.Bounds.Height)
                                                                               .FirstOrDefault();
                ProjectorView          projectorView   =null;
                WpfScreenHelper.Screen primaryScreen   =null;

                if (projectorScreen is null)
                {
#if (RELEASE || PRODTEST)
                    MessageBox.Show("Der er ikke oprettet forbindelse til en sekundær skærm. Tast Win+K", "Info");
#else
                    primaryScreen = WpfScreenHelper.Screen.PrimaryScreen;

                    projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    if (projectorView is null)
                    {
                        await windowManager.ShowWindowAsync(this, "ProjectorView");

                        // erstat kaldet der viser vinduet (i din eksisterende showProjector-metode)
                        //var projectorViewModel = new ProjectorViewModel(this);
                        //await windowManager.ShowWindowAsync(projectorViewModel, "ProjectorView");
                        projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                        projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
                        projectorView.WindowState           = WindowState.Normal;
                        projectorView.Width                 = 800;
                        projectorView.Height                = 600;

                        projectorView.Top  = primaryScreen.WorkingArea.Top;
                        projectorView.Left = primaryScreen.WpfBounds.Left
                                           + primaryScreen.WpfBounds.Width
                                           - projectorView.Width;
                    }

#endif
                }
                else

                {
                    projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    if (projectorView is null)
                    {
                        await windowManager.ShowWindowAsync(this, "ProjectorView");
                        projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();
                    }

                    projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
                    projectorView.Left                  = projectorScreen.WorkingArea.Left;
                    projectorView.Top                   = projectorScreen.WorkingArea.Top;
                    projectorView.Width                 = projectorScreen.WorkingArea.Width;
                    projectorView.Height                = projectorScreen.WorkingArea.Height;
                    projectorView.WindowState           = WindowState.Maximized;
                }

                if (projectorView == null)
                    Logger.Error("Kunne ikke finde eller oprette ProjectorView");
                else
                    Logger.Info($"ProjectorView vist på {(projectorScreen != null ? $"skærm: {projectorScreen.DeviceName}" : "primær skærm")}");

                // Move ShellView activation out here, so that it ALWAS get focus in the end.
                var shellVm   = IoC.Get<ShellViewModel>();
                var shellView = shellVm.GetView() as Window;

                if (shellView != null)
                {
                    shellView.Activate();
                    shellView.Topmost = true;
                    shellView.Topmost = false;
                    shellView.Focus();
                }
            }

            // og tilføj disse private metoder (placer dem f.eks. under andre private helpers)
            private void pairsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyOfPropertyChange(() => Pairs);
            }

            private void teamsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                NotifyOfPropertyChange(() => Teams);
            }

            #region FileWatcher and Handlers
                private async Task handleFileEventAsync(FileSystemEventArgs ev)
                {
                    Logger.Debug($"File Event: {ev.FullPath}");
                    try
                    {
                        if (ev.FullPath.StartsWith(Configuration.HomepagePath))
                            reloadSelectedClub(ev.FullPath);
                        else
                            initWatcher();
                    }

                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error handling event on UI thread: {ex.Message}");
                        ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                    }
                }

                internal void SetBC3Watcher(bool enable)
                {
                    if (enable)
                        loadMainClubs();
                    else
                        SelectedMainClub = null;

                    watcher.EnableRaisingEvents = Configuration.ReadBC3;
                }

                internal void SetBridgeMateWatcher(bool enable)
                {
                    if (enable)
                        BridgeMate.CheckOrOpen(SelectedPlayingTime?.Date, SelectedMainClub?.No);
                    else
                        BridgeMate.Close();

                    //watcher.EnableRaisingEvents = Configuration.ReadBridgeMate;
                }
            #endregion

            #region Handle BridgeMate RoundStatus changes
                //private void roundStatus_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
                //{
                //    if (e.NewItems?[0] is RoundStatus roundStatus
                //    &&  roundStatus.Done)
                //    {
                //        List<BridgeTimer> timers = Configuration.GetRelatedTimers(roundStatus);

                //        foreach (var timer in timers)
                //            timer.Round = roundStatus.Round;
                //    }
                //}
                private void roundStatusItemChanged(object sender, ItemPropertyChangedEventArgs<RoundStatus> e)
                {
                    if (e.PropertyName == nameof(RoundStatus.Done))
                        checkRoundStatus(e.Item);
                }

                private void checkRoundStatus(RoundStatus roundStatus)
                {
                    List<BridgeTimer> timers = Configuration.GetRelatedTimers(roundStatus, threshold);

                    foreach (var timer in timers)
                        if (timer.Round >  roundStatus.Round)
                            timer.Round =  roundStatus.Round;

                    if (roundStatus.Done)
                    {
                        Logger.Debug($"Round done: {roundStatus}");

                        foreach (var timer in timers)
                        {
                            if (BridgeMate.RoundStatus.Where(s =>  s.Round  == roundStatus.Round
                                                               &&  s.Letter == roundStatus.Letter)
                                                      .All(s => s.Done))
                                if (timer.RemainingTime >  TimeSpan.Zero)
                                    timer.FinishRound((int)roundStatus.Round);
                        }
                    }
                }
            #endregion

            #region Overrides
                protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
                {
                    // When viewmodel is closed, dispose resources
                    if (close)
                        Dispose();
                    else
                    {
                        // optional: detach transient handlers if you want while still alive
                    }

                    return base.OnDeactivateAsync(close, cancellationToken);
                }

                // IDisposable pattern
                public void Dispose()
                {
                    Dispose(true);
                    GC.SuppressFinalize(this);
                }

                protected virtual void Dispose(bool disposing)
                {
                    if (disposed)
                        return;

                    disposed = true;

                    if (disposing)
                    {
                        // unsubscribe BridgeMate handlers
                        if (BridgeMate?.RoundStatus is not null)
                            BridgeMate.RoundStatus.ItemChanged -= roundStatusItemChanged;

                        // unsubscribe collection handlers
                        if (Pairs is not null)
                            Pairs.CollectionChanged -= pairsCollectionChanged;

                        if (Teams is not null)
                            Teams.CollectionChanged -= teamsCollectionChanged;

                        // watcher
                        if (watcher is not null)
                        {
                            watcher.UpdatedAsync-= handleFileEventAsync;
                            watcher.Dispose();
                            watcher = null;
                        }
                    }
                }

                ~ControlViewModel()
                {
                    Dispose(false);
                }
            #endregion
        #endregion
    }
}
