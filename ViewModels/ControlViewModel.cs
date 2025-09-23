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
using Baksteen.Extensions.DeepCopy;
using Caliburn.Micro;
using DBF.Converters;
using DBF.DataModel;
using DBF.UserControls;
using DBF.Views;
using PropertyChanged;
using Syncfusion.Data.Extensions;
using Syncfusion.DocIO.DLS;

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
        private readonly IWindowManager                         windowManager;
        private          Club                                   selectedClub;
        private          UserControl                            startListControl = new StartListControl();
        private          UserControl                            timersPanel      = new TimersPanel() { ButtonsVisibility = Visibility.Collapsed };
        private          UserControl                            resultsControl   = new ResultsControl();
        private          UserControl                            currentView;
        private          MainClub                               selectedMainClub;
        private          PlayingTime                            playingTime;
        private          List<Tournament>                       tournaments;
        private          JsonSerializerOptions                  JsonOptions      = new JsonSerializerOptions { Converters = { new DecimalCommaConverter() } };
        private          Encoding                               iso_8859_1       = System.Text.Encoding.GetEncoding("iso-8859-1");
        private          ObservableCollection<PlayingTime>      spilleDage       = [];
        private          FileSystemWatcher                      watcher;
        private readonly ConcurrentDictionary<string, DateTime> lastFileEvent    = new();
        private          int                                    sectionNo;
        private          bool                                   showAsOneGroup   = true;

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
            }
        #endregion

        #region Public Properties
            public UserControl CurrentView
            {
                get=> currentView ?? (currentView = timersPanel);
                set
                {
                    currentView = value;
                    NotifyOfPropertyChange(() => CurrentView);
                }
            }

            public Configuration                  Configuration            { get; set; }
            public BridgeMate                     BridgeMate               { get; set; }

            // MainClubs
            public ObservableCollection<MainClub> MainClubs                { get; set; } = [];

            //
            public MainClub SelectedMainClub
            {
                get=> selectedMainClub;
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
            public ObservableCollection<Club>     Clubs                    { get; set; } = [];

            public Club SelectedClub
            {
                get=> selectedClub;
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
                get=> spilleDage;
                set
                {
                    if (Set(ref spilleDage, value))
                        SelectedPlayingTime = PlayingTimes.Where(pt => pt.Date <= DateTime.Now.Date).FirstOrDefault()
                                           ?? PlayingTimes.Where(pt => pt.Date >  DateTime.Now.Date).LastOrDefault();
                }
            }

            public PlayingTime SelectedPlayingTime
            {
                get=> playingTime;
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
                get=> sectionNo;
                set
                {
                    if (Set(ref sectionNo, value))
                        HideTournamentSummery = SectionNo <  2;
                }
            }

            public bool                           HideTournamentSummery    { get; set; } = false;
            public bool                           HideHacGrp               { get; set; } = true;
            public DateTime                       Date                     { get; set; }
            public List<GroupSection>             GroupSections            { get; set; }
            public BindableCollection<Pair>       Pairs                    { get; set; } = [];
            public BindableCollection<Team>       Teams                    { get; set; } = [];
            public string                         ErrorMessage             { get; set; }
            public bool ShowAsOneGroup
            {
                get=> showAsOneGroup;
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

            public Visibility                     ShowAsOneGroupVisibility { get; set; } = Visibility.Collapsed;
        #endregion

        #region Public Methods
            public void AddTimer()=> Configuration.AddTimer();

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
                    projectorView.Close();
            }

            public override Task<bool> CanCloseAsync(CancellationToken cancellationToken = default)
            {
                if (Configuration.TimersActive)
                {
                    var result = MessageBox.Show("Hvis du lukker vinduet, så nulstilles alle aktive ure. Vil du fortsætte?", "Bekræft", MessageBoxButton.YesNo);
                    return Task.FromResult(result == MessageBoxResult.Yes);
                }
                else
                    return Task.FromResult(true);
            }
        #endregion

        #region Private Method
            private void LoadMainClubs()
            {
                if (string.IsNullOrWhiteSpace(Configuration.HomepagePath)
                || !Directory.Exists(Configuration.HomepagePath))
                {
                    MessageBox.Show($"Mappen: '{Configuration.HomepagePath}' findes ikke", "Fejl");
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
                    MessageBox.Show($"Kan ikke finde Resultater i mappen: {Configuration.HomepagePath}", "Fejl");
                    return;
                }

                MainClubs        = MainClubs.OrderBy(mc => mc.Name).ToObservableCollection();
                SelectedMainClub = MainClubs[0];
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
                bool RoundCompleted   = true;

                try
                {
                    Configuration.StartTime = playingTime.Date;
                    tournaments             = getTournaments(playingTime);

                    if (tournaments.Count == 0)
                        return;

                    GroupSections = getGroupSections(playingTime, tournaments);

                    Date = playingTime.Date;

                    for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                    {
                        var grp = GroupSections[grpNo];

                        if (!grp.Completed)
                            RoundCompleted = false;

                        if (grp.Tournament.TournamentType.Text == "Parturnering")
                        {
                            if (grp.Resultlist is not null)
                            {
                                InterWovenHowell = grp.Tournament.MovementPlan.Contains("Indvævet Howell");
                                Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;

                                foreach (var pair in grp.Resultlist.Pairs)
                                {
                                    pair.Group = grp.Tournament.Title;
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
                                        pair.Group = grp.Tournament.Title;
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
                                        var hac = rnd?.HACResult.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);
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
                                    ErrorMessage = $"Runden d. {earlierSection.DateStr} er endnu ikke afsluttet eller er ikke sendt til hjemmesiden!";
                            }

                            // Setup TournamentRank Rank by Total KP
                            var totalRank = 1;

                            foreach (var team in teams.Where(t => t.Group == grp.Tournament.Title).OrderByDescending(t => t.TotalKP))
                                team.TournamentRank = totalRank++;
                        }
                    }

                    var i = 0;

                    initSubgroups();

                    foreach (var pair in pairs.OrderBy(p => p.Group).ThenBy(p => p.SubGroup).ThenBy(p => p.PairNo))
                        pair.EntryNo = i++;

                    foreach (var team in teams.OrderBy(p => p.Group).ThenBy(t => t.TeamNo))
                        team.EntryNo = i++;
                }
                catch (Exception)
                {
                    ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
                }

                finally
                {
                    Pairs = pairs;
                    Teams = teams;
                }

#if DEBUG
                // BridgeMate lookup
                if (newSession)
                    if (string.IsNullOrEmpty(ErrorMessage)
                    && !RoundCompleted)
                        BridgeMate.CheckOrOpen(SelectedPlayingTime.Date, SelectedMainClub.No);
                    else
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
                                    //if (clubNew.Id == clubOld.Id)
                                    {
                                        // Update PlayingTimes for existing club
                                        var playingTimesNew = (SelectedClub is null
                                                             ? main.Clubs.SelectMany(club => club.MainTournaments)
                                                             : main.Clubs.FirstOrDefault(c => c.Id == SelectedClub.Id)?.MainTournaments)
                                                                          .SelectMany(mt => mt.PlayingTime);

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
                                                            // Update existing tournament fileNew                                             
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

            private void initSubgroups()
            {
                for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                {
                    var grp = GroupSections[grpNo];

                    if (grp.Tournament.TournamentType.Text == "Parturnering"
                    &&  grp.Resultlist                     is not null)
                    {
                        var InterwovenHowell = grp.Tournament.MovementPlan.Contains("Indvævet Howell");
                        var Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
                        var subGroupSize     = grp.Resultlist.Pairs.Count >> 1;
                        var rankA            = 1;
                        var rankB            = 1;

                        foreach (var pair in Pairs.OrderBy(p => p.SectionRank))
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
                            string json = File.ReadAllText(fullPath, iso_8859_1);
                            return JsonSerializer.Deserialize<T>(json, JsonOptions);
                        }
                        else // XML
                        {
                            // Hvis filen ikke findes, returner null
                            string xml = File.ReadAllText(fullPath, iso_8859_1);

                            // Erstat Fjern tag værdier, som kun består af blanke og - tegn
                            xml = Regex.Replace(xml, @">(-|\s)+<", "><");

                            // Erstat kun komma med punkt i decimaltal (fx 123,45 -> 123.45) - erstatter alle komma mellem tal
                            xml = System.Text.RegularExpressions.Regex.Replace(xml, @"(?<=\d),(?=\d)", ".");

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
            #endregion

            #region Look for Folder or file updates
                private void folderUpdated(object sender, FileSystemEventArgs e)
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

                        if (e.ChangeType == WatcherChangeTypes.Changed)
                            //Debug.WriteLine($"File changed: {e.Name}");
                            if (e.Name == "Main.XML")
                                reloadMain();
                            else
                                FetchPlayingTime(false);
                        else
                            if (e.ChangeType == WatcherChangeTypes.Created)
                                Debug.WriteLine($"File created: {e.Name}");
                            else
                                Debug.WriteLine($"Unhandled update: {e.Name} - {e.ChangeType}");
                    }

                    catch (Exception)
                    {
                        ErrorMessage = "Fejl ved læsning af Start- eller Resultatlister";
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
                        projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                        projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
                        projectorView.WindowState           = WindowState.Normal;
                        projectorView.Top                   = primaryScreen.WorkingArea.Top;
                        projectorView.Width                 = 800;
                        projectorView.Height                = 600;
                        projectorView.Left                  = primaryScreen.WpfBounds.Left + primaryScreen.WpfBounds.Width - projectorView.Width;
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

