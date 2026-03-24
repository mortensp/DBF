using System.Collections.Concurrent;
using System.Collections.ObjectModel;
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
using DBF.UserControls;
using DBF.Views;

using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Charts;

namespace DBF.ViewModels
{
    public static class MovementPlans
    {
        public const string InterWovenHowell = "1";
        public const string Howell           = "1";
        public const string Mitchell         = "4";
    }

    public class ControlViewModel : Screen
    {
        private readonly IWindowManager windowManager;
        private          Club           selectedClub;
        private          UserControl    startListControl = new StartListControl();
        private          UserControl    timersPanel      = new TimersPanel()
                                                           {
                                                               ButtonsVisibility = Visibility.Collapsed
                                                           };

        private UserControl           resultsControl = new ResultsControl();
        private UserControl           currentView;
        private MainClub              selectedMainClub;
        private PlayingTime           playingTime;
        private List<Tournament>      tournaments;
        private JsonSerializerOptions JsonOptions    = new JsonSerializerOptions
                                                       {
                                                           Converters = 
                                                           {
                                                                      new DecimalCommaConverter()
                                                                      }
                                                       };

        private Encoding                          iso_8859_1 = System.Text.Encoding.GetEncoding("iso-8859-1");
        private ObservableCollection<PlayingTime> spilleDage = [];
        private FileSystemWatcher                 watcher;

        private int  sectionNo;
        private bool showAsOneGroup = true;

        // Nyttige felter øverst i klassen - bruges til at undgå af XML filerne er i brug

        // Queue to ensure file system events are processed sequentially in the order received
        // We keep only the latest event per path so near-duplicate events are grouped.
        private readonly System.Collections.Concurrent.ConcurrentQueue<string>                           _eventQueue   = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, FileSystemEventArgs> _latestEvents = new();
        private readonly System.Threading.SemaphoreSlim                                                  _queueSignal  = new(0);
        private          int                                                                             _processingEventLoop;

        #region Constructors
            public ControlViewModel(IWindowManager windowManager, Configuration configuration, BridgeMate bridgeMate)
            {
                BridgeMate                          = bridgeMate;
                Configuration                       = configuration;
                this.windowManager                  = windowManager;
                Thread.CurrentThread.CurrentCulture = Global.DkCulture;

                watcher                       = new FileSystemWatcher();
                watcher.NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size;
                watcher.IncludeSubdirectories = false;

                watcher.Changed+= folderUpdated;
                watcher.Created+= folderUpdated;

                Pairs.CollectionChanged+= (s, e) => NotifyOfPropertyChange(() => Pairs);
                Teams.CollectionChanged+= (s, e) => NotifyOfPropertyChange(() => Teams);

                LoadMainClubs();
#if DEBUG
                // For at gøre test nemmere
                SelectedMainClub    = MainClubs.FirstOrDefault(m => m.Name.Contains("Young Sharks"));
                SelectedPlayingTime = PlayingTimes.FirstOrDefault(p => p.DateStr.StartsWith("02-03-2026"));
#endif
            }
        #endregion

        #region Public Properties
            public UserControl CurrentView
            {
                get => currentView ?? (currentView = timersPanel);
                set
                {
                    currentView = value;
                    NotifyOfPropertyChange(() => CurrentView);
                }
            }

            public Configuration                  Configuration { get; set; }
            public BridgeMate                     BridgeMate    { get; set; }

            // MainClubs
            public ObservableCollection<MainClub> MainClubs     { get; set; } = [];

            public MainClub SelectedMainClub
            {
                get => selectedMainClub;
                set
                {
                    try
                    {
                        ErrorMessage = "";

                        if (Set(ref selectedMainClub, value))
                        {
                            watcher.EnableRaisingEvents = false;
                            watcher.Filters.Clear();

                            if (value == null)
                            {
                                PlayingTimes.Clear();
                                SelectedPlayingTime = null;
                            }
                            else
                            {
                                watcher.Path = value.Path;
                                watcher.Filters.Add("Main.XML");
                                watcher.EnableRaisingEvents = true;

                                Clubs    = SelectedMainClub.Clubs?.OrderBy(c => c.Name)
                                                                  .ToObservableCollection();
                                var club = Clubs?.FirstOrDefault();

                                SelectedClub = null; // nødvendig, da club og SelectedClub kun sammenlignes på feltet Id, dvs. kan væe ens.
                                SelectedClub = club;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            }

            // Clubs
            public ObservableCollection<Club> Clubs { get; set; } = [];

            public Club SelectedClub
            {
                get => selectedClub;
                set
                {
                    ErrorMessage = "";

                    if (Set(ref selectedClub, value))
                        if (value is null)
                        {
                            PlayingTimes.Clear();
                            SelectedPlayingTime = null;
                        }
                        else
                            FetchPlayingTimes();
                }
            }

            // PlayingTimes
            public ObservableCollection<PlayingTime> PlayingTimes
            {
                get => spilleDage;
                set
                {
                    if (Set(ref spilleDage, value))
                        SelectedPlayingTime = PlayingTimes.Where(pt => pt.Date <= DateTime.Now.Date).FirstOrDefault()
                                           ?? PlayingTimes.Where(pt => pt.Date >  DateTime.Now.Date).LastOrDefault();
                }
            }

            public PlayingTime SelectedPlayingTime
            {
                get => playingTime;
                set
                {
                    if (Set(ref playingTime, value))
                        if (value is not null)
                            FetchPlayingTime();
                }
            }

            // Other 
            public int SectionNo
            {
                get => sectionNo;
                set
                {
                    if (Set(ref sectionNo, value))
                        HideTournamentSummery = SectionNo <  2;
                }
            }

            public bool                     HideTournamentSummery { get; set; }
            public bool                     ImpsPair              { get; set; }
            public bool                     HideHacGrp            { get; set; } = true;
            public DateTime                 Date                  { get; set; }
            public List<GroupSection>       GroupSections         { get; set; }
            public BindableCollection<Pair> Pairs                 { get; set; } = [];
            public BindableCollection<Team> Teams                 { get; set; } = [];
            public string                   ErrorMessage          { get; set; }
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

            public bool       BC3Available             => SelectedPlayingTime != null;
            public Visibility ShowAsOneGroupVisibility { get; set; } = Visibility.Collapsed;
        #endregion

        #region Public Methods
            public void AddTimer() => Configuration.AddTimer();

            public async void ShowStartList()
            {
                if (CurrentView is not StartListControl)
                    CurrentView = startListControl;

                await ShowProjector();
            }

            public async void ShowBridgeTimers()
            {
                if (CurrentView is not TimersPanel)
                    CurrentView = timersPanel;

                await ShowProjector();
            }

            public async void ShowResults()
            {
                if (CurrentView is not ResultsControl)
                    CurrentView = resultsControl;

                await ShowProjector();
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

            public override async Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
            {
                try
                {
                    if (Configuration.TimersActive
                    &&  Window.GetWindow(CurrentView)?.GetType().Name != "ProjectorView")
                    {
                        // show custom dialog with three choices
                        var owner = Window.GetWindow(CurrentView) ?? Application.Current.MainWindow;
                        var dlg = new DBF.Views.ConfirmCloseDialog("Hvis du lukker vinduet, så nulstilles alle aktive ure. Hvad vil du gøre?");
                        dlg.Owner = owner;
                        dlg.ShowDialog();

                        switch (dlg.Choice)
                        {
                            case DBF.Views.ConfirmCloseChoice.ContinueClose:
                                return await Task.FromResult(true);

                            case DBF.Views.ConfirmCloseChoice.CancelClose:
                                return await Task.FromResult(false);

                            case DBF.Views.ConfirmCloseChoice.SaveTime:
                                Configuration.SaveState();
                                return await Task.FromResult(true);
                        }
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
            private void LoadMainClubs()
            {
                if (string.IsNullOrWhiteSpace(Configuration.HomepagePath)
                || !Directory.Exists(Configuration.HomepagePath))
                {
                    ShowMessageAFewSeconds($"Mappen: '{Configuration.HomepagePath}' findes ikke");
                    //
                    watcher.EnableRaisingEvents   = false;
                    watcher.NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size | NotifyFilters.DirectoryName;
                    watcher.IncludeSubdirectories = true;
                    watcher.Filters.Clear();

                    watcher.Path = @"c:\";
                    watcher.Filters.Add("Resultater_*");
                    watcher.EnableRaisingEvents = true;

                    return;
                }

                foreach (var path in Directory.GetDirectories(Configuration.HomepagePath)
                                              .Select(dir => Path.GetFileName(dir))
                                              .Where(name => name.StartsWith("Resultater_", StringComparison.OrdinalIgnoreCase)))
                {
                    if (int.TryParse(path.Substring(11), out int no))
                    {
                        var mainClub = loadMainClub(no);

                        if (mainClub is not null)
                            MainClubs.Add(mainClub);
                    }
                }

                if (MainClubs.Count == 0)
                {
                    //MessageBox.Show(
                                            
                    ShowMessageAFewSeconds($"Kan ikke finde Resultater i mappen: {Configuration.HomepagePath}");
                    return;
                }

                MainClubs        = MainClubs.OrderBy(mc => mc.Name).ToObservableCollection();
                SelectedMainClub = MainClubs[0];
            }

            private void ShowMessageAFewSeconds(string msg)
            {
                ErrorMessage = msg;
                // Clear the error message after 10 seconds without blocking the UI thread
                _ = Task.Run(async () =>
                {
                    await Task.Delay(10000).ConfigureAwait(false);
                    await Execute.OnUIThreadAsync(() =>
                    {
                        ErrorMessage = string.Empty;
                        return Task.CompletedTask;
                    }).ConfigureAwait(false);
                });
            }

            private void FetchPlayingTimes()
            {
                try
                {
                    ObservableCollection<PlayingTime> playingtimes = [];

                    var mainTournaments = SelectedClub is null
                                        ? SelectedMainClub.Clubs.SelectMany(club => club.MainTournaments)
                                        : SelectedClub.MainTournaments;

                    foreach (var mt in mainTournaments)

                        foreach (var pt in mt.PlayingTime)
                        {
                            pt.MainTournament = mt;
                            playingtimes.Add(pt);
                        }

                    PlayingTimes = playingtimes.OrderByDescending(s => s.Date).ToObservableCollection();
                }

                catch (Exception)
                {
                    PlayingTimes.Clear();
                }
            }

            /// <summary>
            /// Henter XML data for den valgte Spille dag og klokkeslet
            /// </summary>
            private void FetchPlayingTime(bool newSession = true)
            {
                ShowAsOneGroup                 = true;
                HideHacGrp                     = true;
                ShowAsOneGroupVisibility       = Visibility.Collapsed;
                ErrorMessage                   = "";
                BindableCollection<Pair> pairs = [];
                BindableCollection<Team> teams = [];
                Pairs.Clear();
                Teams.Clear();
                bool InterWovenHowell = false;
                bool Mitchell         = false;

                //bool RoundCompleted   = true;
                try
                {
                    Configuration.StartDate = playingTime.Date;
                    tournaments             = getTournaments(playingTime);

                    if (tournaments.Count == 0)
                        return;

                    GroupSections = getGroupSections(playingTime, tournaments);

                    Date = playingTime.Date;

                    for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                    {
                        var grp = GroupSections[grpNo];

                        //if (!grp.Completed)
                        //    RoundCompleted = false;
                        if (grp.Tournament.TournamentType.Text == "Parturnering")
                        {
                            Mitchell = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
                            ImpsPair = grp.Tournament.TournamentPairCalcType == "3";

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
                        else
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
                    }
                }
                catch (Exception)
                {
                    ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                }

                int i =0;

                foreach (var pair in pairs.OrderBy(p => p.Group).ThenBy(p => p.SubGroup).ThenBy(p => p.PairNo))
                    pair.EntryNo = i++;

                foreach (var team in teams.OrderBy(p => p.Group).ThenBy(t => t.TeamNo))
                    team.EntryNo = i++;

                initSubgroups(pairs);
                Pairs = pairs;
                Teams = teams;

#if DEBUG
                // BridgeMate lookup
                if (newSession)
                    //if (string.IsNullOrEmpty(ErrorMessage))
                    //    BridgeMate.CheckOrOpen(SelectedPlayingTime.Date, SelectedMainClub.No);
                    //else
                    BridgeMate.Close();
#endif
                // Restore Taskbar Icon.
                //Execute.OnUIThread(() =>
                //                   {
                //                       Application.Current.MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/DBF_Tools.ico", UriKind.Absolute));
                //                   });
            }

            #region Load and Reload MainClub
                private MainClub loadMainClub(int no)
                {
                    var path     = Configuration.HomepagePath + @$"Resultater_{no}\";
                    var filename = path + @"Main.xml";

                    try
                    {
                        var mainclub = deserialize<MainClub>(filename);

                        if (mainclub.Clubs is null)
                        {
                            System.Threading.Thread.Sleep(1000);
                            mainclub = deserialize<MainClub>(filename);
                        }

                        if (mainclub.Clubs is null)
                        {
                            ErrorMessage = $"Fejl ved læsning af Main.xml";
                            return null;
                        }

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

                private void reloadMain()
                {
                    try
                    {
                        if (SelectedMainClub is not null)
                        {
                            var main = loadMainClub(SelectedMainClub.No);

                            if (main?.Clubs is not null)
                                foreach (var clubNew in main.Clubs)
                                {
                                    var clubOld = Clubs.FirstOrDefault(c => c.Id == clubNew.Id);

                                    if (clubOld is null)
                                        for (var i = 0; i <  Clubs.Count; i++)
                                        {
                                            if (string.Compare(Clubs[i].Name, clubNew.Name, StringComparison.CurrentCulture) <= 0)
                                            {
                                                Execute.OnUIThread(() => Clubs.Insert(i, clubNew));
                                                break;
                                            }
                                        }
                                    else
                                        if (clubNew.Id == SelectedClub?.Id)
                                        //if (clubNew.Id == clubOld.Id)
                                        {
                                            // Update PlayingTimes for current club
                                            //var playingTimesNew = (SelectedClub is null
                                            //                             ? main.Clubs.SelectMany(club => club.MainTournaments) // rammes aldrig
                                            //                             : main.Clubs.FirstOrDefault(c => c.Id == SelectedClub.Id)?.MainTournaments)
                                            //                            .SelectMany(mt => mt.PlayingTime);
                                            var playingTimesNew = SelectedClub.MainTournaments.SelectMany(mt => mt.PlayingTime);

                                            foreach (var playingTimeNew in playingTimesNew)
                                            {
                                                var playingTimeOld = PlayingTimes.FirstOrDefault(pt => pt.Date == playingTimeNew.Date);

                                                if (playingTimeOld is null)
                                                    for (var i = 0; i <  PlayingTimes.Count; i++)
                                                    {
                                                        if (PlayingTimes[i].Date <  playingTimeNew.Date)
                                                        {
                                                            Execute.OnUIThread(() => PlayingTimes.Insert(i, playingTimeNew));
                                                            break;
                                                        }
                                                    }
                                                else
                                                    if (playingTimeOld.Date == playingTimeNew.Date)
                                                    {
                                                        foreach (var fileNew in playingTimeNew.TournamentFiles)
                                                        {
                                                            var fileOld = playingTimeOld.TournamentFiles.FirstOrDefault(o => o.FileName == fileNew.FileName);

                                                            if (fileOld is null)
                                                            {
                                                                foreach (var file in playingTimeOld.TournamentFiles)
                                                                    watcher.Filters.Remove(file.FileName);

                                                                Execute.OnUIThread(() => playingTimeOld.TournamentFiles = playingTimeNew.TournamentFiles);
                                                                SelectedPlayingTime = null;
                                                                SelectedPlayingTime = playingTimeOld;
                                                                return;
                                                            }
                                                            else
                                                                // UpdateAndMarkAppStarted existing tournament fileNew                                             
                                                                fileOld.Merge(fileNew);
                                                        }

                                                        //if (updatedFile)
                                                        //{
                                                        //    SelectedPlayingTime = null;
                                                        //    SelectedPlayingTime = playingTimeOld;
                                                        //}
                                                    }
                                            }
                                        }
                                }
                        }
                    }
                    catch (Exception)
                    {
                        ErrorMessage = "Fejl ved læsning af Main.xml";
                    }
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
                    foreach (var pathName in watcher.Filters.Where(f => f.StartsWith("MT")).ToList())
                        watcher.Filters.Remove(pathName);

                    List<Tournament> tournaments = new();

                    if (pt is null || pt.TournamentFiles is null)
                        ErrorMessage = $"Data for er ikke sendt til hjemmesiden";
                    else
                        foreach (var tournamentFile in pt.TournamentFiles)
                        {
                            var path       = SelectedMainClub.Path + tournamentFile.FileName;
                            var tournament = deserialize<Tournament>(path);

                            watcher.Filters.Add(tournamentFile.FileName);

                            if (tournament is null)
                                if (string.IsNullOrEmpty(ErrorMessage))
                                    ErrorMessage = $"Data for '{tournamentFile.GroupName}' er ikke sendt til hjemmesiden";
                                else
                                    ErrorMessage = $"Data er ikke sendt til hjemmesiden";
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

                private GroupSection getGroupSection(String fileName, Tournament tournament)
                {
                    var path    = SelectedMainClub.Path + fileName;
                    var section = deserialize<GroupSection>(path);

                    watcher.Filters.Add(fileName);

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
                            string json = ReadAllTextWithRetry(fullPath, iso_8859_1);
                            return JsonSerializer.Deserialize<T>(json, JsonOptions);
                        }
                        else // XML
                        {
                            // Hvis filen ikke findes, returner null
                            //string xml = File.ReadAllText(fullPath, iso_8859_1);
                            string xml = ReadAllTextWithRetry(fullPath, iso_8859_1);

                            // Erstat Fjern tag værdier, som kun består af blanke og - tegn
                            xml = Regex.Replace(                     xml,       @">(-|\s)+<",                 "><");

                            // Erstat kun komma med punkt i decimaltal (fx 123,45 -> 123.45) - erstatter alle komma mellem tal
                            xml = Regex.Replace(                     xml,       @"(?<=\d),(?=\d)",            ".");

                            // Remove empty tags
                            xml = Regex.Replace(                     xml,       @"<(\w+)(\s[^>]*)?>\s*</\1>", string.Empty); //<TagName></TagName>
                            xml = Regex.Replace(                     xml,       @"<\w+\s*(/>|/>\s*</\1*s>)",  string.Empty); //<TagName/>

                            var       serializer = new XmlSerializer(typeof(T));
                            using var reader     = new StringReader( xml);
                            return (T)serializer.Deserialize(        reader);
                        }
                    }
                    catch (Exception)
                    {
                        ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                        return new T();
                    }
                }

                // Tilføj denne private helper-metode i samme klasse (f.eks. nederst i filen)
                private string ReadAllTextWithRetry(string path, Encoding encoding, int maxAttempts = 10, int initialDelayMs = 100)
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

            #region Look for Folder or file updates
                // Erstat din eksisterende folderUpdated-metode med denne
                // Denne version sikrer at events ikke springes over og behandles sekventielt
                private void folderUpdated(object sender, FileSystemEventArgs e)
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
                        ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
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

                                // Execute handling on UI thread (same behaviour as tidligere)
                                await Execute.OnUIThreadAsync(async () =>
                                {
                                    try
                                    {
                                        if (ev.ChangeType == WatcherChangeTypes.Changed)
                                            if (ev.Name == "Main.XML")
                                                reloadMain();
                                            else
                                                FetchPlayingTime(false);
                                        else
                                            if (ev.ChangeType == WatcherChangeTypes.Created)
                                                if (ev.FullPath.StartsWith(Configuration.HomepagePath))
                                                {
                                                    await Task.Delay(7000); // 7 seconds delay to allow file to be fully written and stable before processing (especially important for Main.xml which triggers a full reload)
                                                    LoadMainClubs();
                                                }
                                                else
                                                    Debug.WriteLine($"File Created: {ev.Name}");
                                            else
                                                Debug.WriteLine($"File {ev.ChangeType}: {ev.Name}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error handling event on UI thread: {ex.Message}");
                                    }
                                    return;
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

            private async Task ShowProjector()
            {
                var projectorScreen = WpfScreenHelper.Screen.AllScreens
                                                            .Where(s => !s.Primary)
                                                            .OrderByDescending(s => s.Bounds.Width * s.Bounds.Height)
                                                            .FirstOrDefault();

                if (projectorScreen is null)
                {
#if RELEASE
                    MessageBox.Show("Der er ikke oprettet forbindelse til en sekundær skærm. Tast Win+K", "Info");
#else
                    var primaryScreen = WpfScreenHelper.Screen.PrimaryScreen;

                    var projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    if (projectorView is null)
                    {
                        await windowManager.ShowWindowAsync(this, "ProjectorView");

                        // erstat kaldet der viser vinduet (i din eksisterende ShowProjector-metode)
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
                    var projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

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
        #endregion
    }
}
