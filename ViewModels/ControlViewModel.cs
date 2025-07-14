using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Baksteen.Extensions.DeepCopy;
using Caliburn.Micro;
using CData.EntityFrameworkCore.Access;
using CData.Sql;
using DBF.BridgeMateModel;
using DBF.DataModel;
using DBF.UserControls;
using DBF.Views;
using Microsoft.DotNet.DesignTools.Protocol.Values;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Syncfusion.Data.Extensions;
using Syncfusion.XlsIO.Parser.Biff_Records;

namespace DBF.ViewModels
{
    public class PlayerName
    {
        public int    Id    { get; set; }
        public string Name  { get; set; }
        public string strID { get; set; }
    }

    public class ControlViewModel : Screen
    {
        static string BC3path = @"C:\BC3\Hjemmeside\";
        //static string path = @"C:\BC3\Hjemmeside - Kopi\Resultater_2172\";
        private readonly IWindowManager _windowManager;

        private Club              selectedClub;
        private UserControl       startListControl = new StartListControl();
        private UserControl       timersPanel      = new TimersPanel() { ButtonsVisibility = Visibility.Collapsed };
        private UserControl       resultsControl   = new ResultsControl();
        private UserControl       currentView;
        private MainClub          selectedMainClub;
        private PlayingTime       playingTime;
        private List <Tournament> tournaments;

        private          JsonSerializerOptions                   JsonOptions    = new JsonSerializerOptions { Converters = { new DecimalCommaConverter() } };
        private          Encoding                                iso_8859_1     = System.Text.Encoding.GetEncoding("iso-8859-1");
        private          ObservableCollection <PlayingTime>      spilleDage     = [];
        private          FileSystemWatcher                       watcher;
        private          int                                     sectionNo;
        private readonly ConcurrentDictionary <string, DateTime> _lastFileEvent = new();

        #region Constructors
            public ControlViewModel(IWindowManager windowManager, Configuration configuration)
            {
                Configuration                       = configuration;
                _windowManager                      = windowManager;
                Thread.CurrentThread.CurrentCulture = Global.DkCulture;

                watcher                       = new FileSystemWatcher();
                watcher.NotifyFilter          = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size;
                watcher.IncludeSubdirectories = false;

                watcher.Changed += folderUpdated;
                watcher.Created += folderUpdated;

                //using (var db = new AccessContext())
                //{
                //    var list = db.Tables.ToList();
                //}
                LoadMainClub();
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

            public Configuration                  Configuration         { get; set; }

            // MainClubs
            public ObservableCollection<MainClub> MainClubs             { get; set; } = [];
            //
            public MainClub SelectedMainClub
            {
                get => selectedMainClub;
                set
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

                            Clubs = SelectedMainClub.Clubs?.OrderBy(c => c.Name)
                                                           .ToObservableCollection();

                            if (Clubs is not null)
                                SelectedClub = Clubs[0];
                        }
                    }
                }
            }

            // Clubs
            public ObservableCollection<Club>     Clubs                 { get; set; } = [];
            //
            public Club SelectedClub
            {
                get => selectedClub;
                set
                {
                    ErrorMessage = "";

                    //playingTime = null;
                    if (Set(ref selectedClub, value))
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
                        SelectedPlayingTime = PlayingTimes.FirstOrDefault();
                }
            }

            //
            public PlayingTime SelectedPlayingTime
            {
                get => playingTime;
                set
                {
                    if (Set(ref playingTime, value))
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

            public bool                           HideTournamentSummery { get; set; } = false;
            public DateTime             Date { get; set; }
            public List<GroupSection> GroupSections { get; set; }
            public BindableCollection<Pair>       Pairs                 { get; set; } = [];
            public BindableCollection<Team>       Teams                 { get; set; } = [];
            public string                         ErrorMessage          { get; set; }
        #endregion

        #region Public Methods
            public void AddTimer() => Configuration.AddTimer();

            public void Help() { }

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
        #endregion

        #region Private Method
            private void LoadMainClub()
            {
                foreach (var path in Directory.GetDirectories(BC3path)
                                              .Select(dir => Path.GetFileName(dir))
                                              .Where (name => name.StartsWith("Resultater_", StringComparison.OrdinalIgnoreCase)))
                {
                    if (int.TryParse(path.Substring(11), out int no))
                    {
                        var mainClub = loadMainClub(no);
                        MainClubs.Add(mainClub);
                    }
                }

                if (MainClubs.Count == 0)
                {
                    MessageBox.Show($"Kan ikke finde Resultater i mappen: {BC3path}", "Fejl");
                    return;
                }

                SelectedMainClub = MainClubs[0];
            }

            private void FetchPlayingTimes()
            {
                PlayingTimes = SelectedClub is null
                             ? SelectedMainClub.Clubs
                                               .SelectMany(club => club.MainTournaments)
                                               .SelectMany(mt => mt.PlayingTime).OrderByDescending(s => s.Date).ToObservableCollection()
                             : SelectedClub.MainTournaments
                                           .SelectMany(mt => mt.PlayingTime).OrderByDescending(s => s.Date).ToObservableCollection();
            }

            /// <summary>
            /// Henter XML data for den valgte Spille dag og klokkeslet
            /// </summary>
            private void FetchPlayingTime()
            {
                ErrorMessage = "";
                Pairs.Clear();
                Teams.Clear();

                tournaments = getTournaments(playingTime);

                if (tournaments.Count == 0)
                    return;

                GroupSections = getGroupSections(playingTime, tournaments);

                Date = playingTime.Date;

                //foreach (var grp in GroupSections)
                for (var grpNo = 0; grpNo <  GroupSections.Count; grpNo++)
                {
                    var grp = GroupSections[grpNo];

                    if (grp.Tournament.TournamentType.Text == "Parturnering")
                    {
                        if (grp.Resultlist is not null)
                            foreach (var pair in grp.Resultlist.Pairs)
                            {
                                pair.Title = grp.Tournament.Title;
                                Pairs.Add(pair.DeepCopy());
                            }

                        if (grp.Startlist is not null)
                            foreach (var pair in grp.Startlist.Pairs)
                            {
                                var res = Pairs.FirstOrDefault(p => p.Title == grp.Tournament.Title && p.PairNo == pair.PairNo);

                                if (res is null)
                                {
                                    pair.Title = grp.Tournament.Title;
                                    Pairs.Add(pair.DeepCopy());
                                }
                                else
                                    res.StartPos = pair.StartPos;
                            }

                        if (Pairs.Count >  0)
                            Pairs = new BindableCollection<Pair>(Pairs.OrderBy(p => p.SectionRank));
                    }
                    else
                    {
                        foreach (var team in grp.Rounds[0].Startlist.Teams)
                        {
                            team.Title = grp.Tournament.Title;
                            Teams.Add(team);
                        }

                        // Merge the four lists
                        var rnd = grp.Rounds[^1];

                        if (rnd is not null && rnd.RoundCompleted)
                            foreach (var team in Teams.Where(t => t.Title == grp.Tournament.Title))
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

                        foreach (var sectionFile in tournaments[grpNo].SectionFiles.Where(f => f.No <  grp.SectionNo))
                        {
                            var earlierSection = getGroupSection(sectionFile.FileName, grp.Tournament);

                            rnd = earlierSection.Rounds[^1];

                            if (rnd is not null && rnd.RoundCompleted)
                                foreach (var team in Teams.Where(t => t.Title == grp.Tournament.Title))
                                {
                                    var res = rnd.Resultlist.Teams.FirstOrDefault(t => t.TeamNo == team.TeamNo);

                                    if (res is not null)
                                        team.TotalKP += res.KP ?? 0;
                                }
                            else
                                ErrorMessage = $"Runden d. {earlierSection.DateStr} er endnu ikke afsluttet eller er ikke sendt til hjemmesiden!";
                        }

                        // Sort teams by TotalKP
                        var totalRank = 1;

                        foreach (var team in Teams.Where(t => t.Title == grp.Tournament.Title).OrderByDescending(t => t.TotalKP))
                            team.TournamentRank = totalRank++;
                    }

                    if (Teams.Count >  0)
                        Teams = new BindableCollection<Team>(Teams.OrderBy(t => t.TournamentRank));
                }

                NotifyOfPropertyChange(nameof(Pairs));
                NotifyOfPropertyChange(nameof(Teams));

                // Restore Taskbar Icon.
                Execute.OnUIThread(() =>
                                   {
                                       Application.Current.MainWindow.Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Images/DBF_Tools.ico", UriKind.Absolute));
                                   });
            }

            private MainClub loadMainClub(int no)
            {
                var path     = BC3path + @$"Resultater_{no}\";
                var mainclub = deserialize<MainClub>(path + @"Main.xml");

                if (mainclub.Clubs is null)
                {
                    System.Threading.Thread.Sleep(1000);
                    mainclub = deserialize<MainClub>(path + @"Main.xml");
                }

                mainclub.Path = path;
                mainclub.No   = no;

                return mainclub;
            }

            private void reloadMain()
            {
                try
                {
                    if (SelectedMainClub is not null)
                    {
                        var main = loadMainClub(SelectedMainClub.No);

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
                                                     ? main.Clubs.SelectMany    (club => club.MainTournaments)
                                                     : main.Clubs.FirstOrDefault(c => c.Id == SelectedClub.Id)?.MainTournaments)
                                                                  .SelectMany    (mt => mt.PlayingTime);

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
                                            //var updatedFile = false;
                                            //var date        = new DateTime(2025, 04, 30, 18, 45, 0);
                                            //var oldDate     = playingTimeOld.Date;

                                            //if (oldDate == date)
                                            //    Debugger.Break();
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

                catch (Exception ex)
                {
                    throw ex;
                }
            }

            private List<Tournament> getTournaments(PlayingTime pt)
            {
                foreach (var pathName in watcher.Filters.Where(f => f.StartsWith("MT")).ToList())
                    watcher.Filters.Remove(pathName);

                List <Tournament> tournaments = new();

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
                List <GroupSection> sections = new();
                SectionNo                    = 1;

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

            private void folderUpdated(object sender, FileSystemEventArgs e)
            {
                // Debounce: Ignorer events for samme fil inden for 500 ms
                var now = DateTime.UtcNow;

                if (_lastFileEvent.TryGetValue(e.FullPath, out DateTime last))
                    if ((now - last).TotalMilliseconds <  500)
                        return; // Ignorer duplikat
                    else
                        _lastFileEvent[e.FullPath] = now;
                else
                    _lastFileEvent.TryAdd(e.FullPath, now);

                if (e.ChangeType == WatcherChangeTypes.Changed)
                    //Debug.WriteLine($"File changed: {e.Name}");
                    if (e.Name == "Main.XML")
                        reloadMain();
                    else
                        FetchPlayingTime();
                else
                    if (e.ChangeType == WatcherChangeTypes.Created)
                        Debug.WriteLine($"File created: {e.Name}");
                    else
                        Debug.WriteLine($"Unhandled update: {e.Name} - {e.ChangeType}");
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

                        // Remove empty tags: <TagName></TagName> and <TagName/>
                        xml = Regex.Replace(xml, @"<(\w+)(\s[^>]*)?>\s*</\1>", string.Empty);
                        xml = Regex.Replace(xml, @"<\w+\s*(/>|/>\s*</\1*s>)" , string.Empty);

                        var       serializer = new XmlSerializer(typeof(T));
                        using var reader     = new StringReader(xml);
                        return (T)serializer.Deserialize(reader);
                    }
                }

                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    return new T();
                }
            }

            //[DllImport("user32.dll")]             //public static extern int ChangeDisplaySettingEx(string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd, int dwflags, IntPtr lParam);
            private async Task ShowProjector()
            {
                var screens = WpfScreenHelper.Screen.AllScreens.ToList();

#if RELEASE
                if (screens.Count <  2)
                    MessageBox.Show("Der er ikke oprettet forbindelse til en sekundær skærm. Tast Win+K", "Info");
                else
#endif
                {
                    var screenTwo = screens[^1]; // Get the second screen (index 1)

                    var projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    if (projectorView is null)
                    {
                        await _windowManager.ShowWindowAsync(this, "ProjectorView");
                        projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();
                    }

                    projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
                    projectorView.Left                  = screenTwo.WorkingArea.Left;
                    projectorView.Top                   = screenTwo.WorkingArea.Top;
                    projectorView.Width                 = screenTwo.WorkingArea.Width;
                    projectorView.Height                = screenTwo.WorkingArea.Height;
                    projectorView.WindowState           = WindowState.Maximized;
                }

                // Flyt ShellView-aktivering herud, så den ALTID får fokus til sidst
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

            //private void bringViewToFront(string vm = null)
            //{
            //    var shellView = Application.Current.Windows
            //                                       .OfType<DBF.Views.ShellView>()
            //                                       .FirstOrDefault();

            //    if (shellView != null)
            //    {
            //        if (shellView.WindowState == WindowState.Minimized)
            //            shellView.WindowState =  WindowState.Normal;

            //        shellView.Activate();
            //        shellView.Topmost = true;
            //        shellView.Topmost = false;
            //        shellView.Focus();
            //    }
            //}

            //private void bringShellViewToFront()
            //{
            //    var shellVm   = IoC.Get<ShellViewModel>();
            //    var shellView = shellVm.GetView() as Window;

            //    if (shellView != null)
            //    {
            //        shellView.Activate();
            //        shellView.Topmost = true;
            //        shellView.Focus();
            //        shellView.Topmost = false;
            //    }
            //}
        #endregion
    }
}

