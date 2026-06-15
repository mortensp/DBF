using Caliburn.Micro;

using DBF.AudioServices;
using DBF.Converters;
using DBF.DataModel;
using DBF.Helpers;
using DBF.UserControls;
using DBF.Views;

using String.Localization;

using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Serialization;

namespace DBF.ViewModels;

public class ControlViewModel : Screen, IDisposable
{
    #region Private Fields
    private                 SerializedFileSystemWatcher _watcher;
    private static readonly TimeSpan                    _threshold   = new TimeSpan(0, 0, 10);
    private                 bool                        _disposed;
    private                 Encoding                    _iso_8859_1  = Encoding.GetEncoding("iso-8859-1");
    private                 JsonSerializerOptions       _jsonOptions = new()
    {
        Converters = { new DecimalCommaConverter() }
    };

    private          LexStrings                      _lexStrings       ;
    private          BindableCollection<PlayingTime> _playingDates     = [];
    private          PlayingTime                     _playingTime;
    private          UserControl                     _resultsControl   = new ResultsControl();
    private          int                             _sectionNo;
    private          Club                            _selectedClub;
    private          MainClub                        _selectedMainClub;
    private          bool                            _showAsOneGroup   = true;
    private          UserControl                     _startListControl = new StartListControl();
    private          TimersPanel                     _timersPanel      =new();
    private          List<Tournament>                _tournaments;
    private readonly IWindowManager                  _windowManager;
    #endregion

    #region Constructors
    public ControlViewModel(IWindowManager windowManager, Configuration configuration, BridgeMate bridgeMate)
    {
        try
        {
            _lexStrings = new(this);

            BridgeMate = bridgeMate;

            if (BridgeMate?.RoundStatus is not null)
                BridgeMate.RoundStatus.ItemChanged += roundStatusItemChanged;

            Configuration = configuration;

            _windowManager = windowManager;
            _watcher = new() { EventGroupingDelay = TimeSpan.FromMilliseconds(7000) };

            _watcher.UpdatedAsync += handleFileEventAsync;
            //
            Pairs.CollectionChanged += pairsCollectionChanged;
            Teams.CollectionChanged += teamsCollectionChanged;
            //
            Configuration.PropertyChanged += configurationPropertyChanged;
            //
            _timersPanel.SetBinding(TimersPanel.OrientationProperty
                                   , new Binding(nameof(Configuration.WindowOrientation))
                                   {
                                       Source = Configuration
                                       ,
                                       Mode = System.Windows.Data.BindingMode.OneWay
                                   }

                                   );

            _timersPanel.SetBinding(TimersPanel.BridgeTimersProperty
                                   , new Binding(nameof(Configuration.BridgeTimers))
                                   {
                                       Source = Configuration
                                       ,
                                       Mode = System.Windows.Data.BindingMode.OneWay
                                   }

                                   );
            //
            initWatcher();
            loadMainClubs();
        }
        catch (Exception ex)
        {
            Logger.Exception(ex);
        }
    }
    #endregion

    #region Public Properties
    public Configuration Configuration { get; set; }

    public BridgeMate BridgeMate { get; set; }

    public bool HideTournamentSummery { get; set; }

    public bool ImpsPair { get; set; }

    public bool HideHac { get; set; } = false;

    public bool HideHacGrp { get; set; } = true;

    public DateTime Date { get; set; }

    public List<GroupSection> GroupSections { get; set; }

    public BindableCollection<Pair> Pairs { get; set; } = [];

    public BindableCollection<Team> Teams { get; set; } = [];

    public Visibility ShowAsOneGroupVisibility { get; set; } = Visibility.Collapsed;

    public UserControl CurrentView { get; private set; }

    public bool BC3Available => SelectedPlayingTime != null;

    public ObservableCollection<SortColumnDescription> SharedSortDescriptions { get; } = new();

    public int SectionNo
    {
        get => _sectionNo;
        set
        {
            if (Set(ref _sectionNo, value))
                HideTournamentSummery = SectionNo < 2;
        }
    }

    public LexString ErrorMessage { get => field ?? (field = new LexString()); set => Set(ref field, value); }

    public bool ShowAsOneGroup
    {
        get => _showAsOneGroup;
        set
        {
            //var old = _showAsOneGroup;
            if (Set(ref _showAsOneGroup, value))
                if (value == true)
                    foreach (var pair in Pairs)
                    {
                        pair.SubGroup = string.Empty;
                        pair.Position = pair.SectionRank;
                    }
                else
                    initSubgroups();

            Pairs = new(Pairs); // For at sikre at UI opdateres, da SubGroup og Position ændres for alle par
        }
    }

    #region Main Club(s)
    public ObservableCollection<MainClub> MainClubs { get; set; } = [];

    public MainClub SelectedMainClub
    {
        get => _selectedMainClub;
        set
        {
            _lexStrings.Set(ErrorMessage, () => string.Empty);

            if (Set(ref _selectedMainClub, value))
                if (value == null)
                {
                    PlayingTimes.Clear();
                    SelectedPlayingTime = null;
                }
                else
                {
                    Clubs = SelectedMainClub.Clubs?.OrderBy(c => c.Name, StringComparer.Create(Global.DkCulture, true))
                                                   .ToObservableCollection();

                    Logger.Info($"SectedMainClub changed: {value}");

                    SelectedClub = null; // Needed as club and SelectedClub are only compared by the Id field, so they can be the same.
                    SelectedClub = Clubs?.FirstOrDefault();
                }
        }
    }
    #endregion

    #region SubClub(s)
    public ObservableCollection<Club> Clubs { get; set; } = [];

    public Club SelectedClub
    {
        get => _selectedClub;
        set
        {
            _lexStrings.Set(ErrorMessage, () => string.Empty);

            if (Set(ref _selectedClub, value))
                if (value is null)
                {
                    Logger.Debug($"SectedClub cleared");
                    PlayingTimes.Clear();
                }
                else
                {
                    Logger.Info($"SectedClub changed: {value?.ToString() ?? "Null"}");
                    fetchPlayingTimes();
                }
        }
    }
    #endregion

    #region PlayingTime(s)
    public BindableCollection<PlayingTime> PlayingTimes
    {
        get => _playingDates;
        set
        {
            var before = DateTime.Now.Date.AddDays(1);
            var after  = DateTime.Now.Date.AddDays(-6);

            if (Set(ref _playingDates, value))
                SelectedPlayingTime = PlayingTimes.Where(pt => pt.Date <= before && pt.Date > after).FirstOrDefault() ??
                                    PlayingTimes.Where(pt => pt.Date > DateTime.Now.Date).LastOrDefault() ??
                                    PlayingTimes.FirstOrDefault();
        }
    }

    public PlayingTime SelectedPlayingTime
    {
        get => _playingTime;
        set
        {
            if (Set(ref _playingTime, value))
            {
                BridgeMate?.Close();

                if (value is not null)
                {
                    fetchPlayingTime();

                    //_watcher.EnableRaisingEvents = true;
                    if (_tournaments.Count > 0 && Configuration.ReadBridgeMate)
                    {
                        BridgeMate.CheckOrOpen(SelectedPlayingTime.Date, SelectedMainClub.No);

                        if (File.Exists(Configuration.StatePath))
                            Configuration.DeleteState();
                        else
                        {
                            // List of RoundStatus entries that's Done and only with enties
                            // that have the highest Round Number for each Section
                            var highestDonePerSection = BridgeMate.RoundStatus
                                                                          .Where(r => r.Done)
                                                                          .GroupBy(r => r.Section)
                                                                          .SelectMany(
                                                                                                                                                                                                                                                                                                    g =>
                                                                                                                                                                                                                                                                                                    {
                                                                                                                                                                                                                                                                                                        var maxRound = g.Max(x => x.Round);
                                                                                                                                                                                                                                                                                                        return g.Where(x => x.Round == maxRound);
                                                                                                                                                                                                                                                                                                    })
                                                                          .ToList();

                            foreach (var timer in Configuration.BridgeTimers
                                                               .Where(t => t.Visibility == Visibility.Visible))
                                timer.Reset(false);

                            foreach (var rs in highestDonePerSection.Where(rs => rs.RemainingBoards > 0))
                                foreach (var timer in Configuration.GetRelatedTimers(rs))//, _threshold))
                                    timer.SetRound(rs.Round + 1);
                        }
                    }
                }
            }
        }
    }
    #endregion
    #endregion

    #region Public Methods
    public void Test() { Debugger.Break(); }

    public void LexRefresh()
    {
        _lexStrings.RefreshAll();
        Configuration.UpdateTimers();
    }

    public void AddTimer()
    {
        Configuration.AddTimer();
    }

    public void ToggleWindowOrientation()
    {
        Configuration.WindowOrientation = Configuration.WindowOrientation == Orientation.Horizontal
                                        ? Orientation.Vertical
                                        : Orientation.Horizontal;
        Configuration.Save();
    }

    #region Public Projector Methods
    public async Task ShowStartListAsync()
    {
        if (CurrentView is not StartListControl)
            CurrentView = _startListControl;

        await showProjector().ConfigureAwait(false);
    }

    public async Task ShowBridgeTimersAsync()
    {
        if (CurrentView is not TimersPanel)
            CurrentView = _timersPanel;

        await showProjector().ConfigureAwait(false);
    }

    public async Task ShowResultsAsync()
    {
        if (CurrentView is not ResultsControl)
            CurrentView = _resultsControl;

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
            var owner = CurrentView is not null
                          ? Window.GetWindow(CurrentView) ?? Application.Current.MainWindow
                          : Application.Current.MainWindow;

            if (Configuration.TimersActive && (owner.GetType().Name != "ProjectorView"))
            {
                // show custom dialog with three choices
                var dlg   = new ConfirmCloseDialog($"{Lex.ClosingTheWindow} {Lex.ContinueQuestion}");
                dlg.Owner = owner;
                dlg.ShowDialog();

                switch (dlg.Choice)
                {
                    case ConfirmCloseChoice.Cancel:
                        return await Task.FromResult(false).ConfigureAwait(false);

                    case ConfirmCloseChoice.Close:
                        Configuration.DeleteState();
                        //return await Task.FromResult(true);
                        break;

                    case ConfirmCloseChoice.SaveState:
                        Configuration.StopAll();
                        Configuration.SaveState();
                        //return await Task.FromResult(true);
                        break;
                }
            }
            else
            {
                Configuration.DeleteState();
                //return await Task.FromResult(true);
            }
        }
        catch (Exception ex)
        {
            Logger.Exception(ex, $"Error when closing the ControlViewModel");
        }

        Dispose();  // DISPOSE HER

        return await Task.FromResult(true).ConfigureAwait(false);
    }

    public string MainClubBadge { get; set; }

    public string SubClubBadge { get; set; }

    public string DateBadge { get; set; }
    #endregion

    #region Private Method
    private void initWatcher()
    {
        Logger.Info($"Init Watcher");

        _watcher.Path = Configuration.HomePagePath.FindDeepestExistingDirectory();

        if (string.Compare($"{_watcher.Path}\\", Configuration.HomePagePath, StringComparison.Ordinal) == 0)
        {
            _watcher.Filters.Clear();
            _watcher.LikeFilters.Add(@"Resultater_????*");
            _watcher.Filters.Add("Main.XML");
            _watcher.IncludeSubdirectories = Configuration.ReadBC3;
        }
        else
        {
            _watcher.Filter = Configuration.HomePagePath.FirstNonSharedDirectory(_watcher.Path);
            _watcher.IncludeSubdirectories = true;
        }

        if (Configuration.ReadBC3)
            Logger.Info($"Files Watcher set up on path: {_watcher.Path}");
        else
            Logger.Info($"Files Watcher on path: {_watcher.Path} is disabled");
    }

    private void showMessageAFewSeconds(string msg)
    {
        Logger.Info(msg);
        _lexStrings.Set(ErrorMessage, () => msg);

        _ = Task.Run(
            async () =>
            {
                await Task.Delay(20000).ConfigureAwait(false);
                await Execute.OnUIThreadAsync(
                    () =>
                {
                    if (ErrorMessage.Value == msg)
                        _lexStrings.Set(ErrorMessage, () => string.Empty);
                    return Task.CompletedTask;
                })
                             .ConfigureAwait(false);
            });
    }

    private void showBadge(Action<string> setter, string message, int delayMs = 60000)
    {
        var _player = IoC.Get<IAudioService>();
        _player.Play(AudioResources.Sound_Notify);

        setter(message);
    }

    public void ResetBadges()
    {
        MainClubBadge = SubClubBadge = DateBadge = null;
    }

    private void fetchPlayingTimes()
    {
        try
        {
            var mainTournaments = SelectedClub is null
                                    ? SelectedMainClub.Clubs.SelectMany(club => club.MainTournaments)
                                    : SelectedClub.MainTournaments;

            PlayingTimes = new BindableCollection<PlayingTime>(
                         mainTournaments.SelectMany(
                                                    mt => mt.PlayingTimes
                                                   , (mt, pt) =>
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
    private void fetchPlayingTime()
    {
        ShowAsOneGroup = true;
        HideHacGrp = true;
        ShowAsOneGroupVisibility = Visibility.Collapsed;
        _lexStrings.Set(ErrorMessage, () => string.Empty);
        BindableCollection<Pair> pairs = [];
        BindableCollection<Team> teams = [];

        //bool RoundCompleted = true;
        try
        {
            Logger.Info($"Loading Playing Section: {_playingTime}");
            Configuration.StartDate = _playingTime.Date;
            _tournaments = getTournaments(_playingTime);

            if (_tournaments.Count == 0)
                return;

            GroupSections = getGroupSections(_playingTime, _tournaments);
            Date = _playingTime.Date;
            HideHac = !_tournaments[0].CalculateHAC;

            for (var grpNo = 0; grpNo < GroupSections.Count; grpNo++)
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
            _lexStrings.Set(ErrorMessage, () => Lex.BC3ReadError);
        }

        // Assign EntryNo for sorting in UI
        int i = 0;

        foreach (var pair in pairs.OrderBy(p => p.Group).ThenBy(p => p.SubGroup).ThenBy(p => p.PairNo))
            pair.EntryNo = i++;

        foreach (var team in teams.OrderBy(p => p.Group).ThenBy(t => t.TeamNo))
            team.EntryNo = i++;

        initSubgroups(pairs);
        Pairs = pairs;
        Teams = teams;
        //var pairsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Pairs);
        //using (pairsView.DeferRefresh())
        //{
        //    Pairs.Clear();
        //    Pairs.AddRange(pairs);
        //}

        //var teamsView = System.Windows.Data.CollectionViewSource.GetDefaultView(Teams);

        //using (teamsView.DeferRefresh())
        //{
        //    Teams.Clear();
        //    Teams.AddRange(teams);
        //}
        Logger.Info($"Loaded  Playing Section: {_playingTime}");
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
            _lexStrings.Set(ErrorMessage, () => Lex.SectionNotCompletedOrNotSent);

        // Add KP from earlier sections
        foreach (var sectionFile in _tournaments[grpNo].SectionFiles.Where(f => f.No < grp.SectionNo))
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
                    _lexStrings.Set(ErrorMessage, () => $"{Lex.aPriorSection} {Lex.UnfinishedSection}");
                else
                    _lexStrings.Set(
                                     ErrorMessage
                                   , () => $"{Lex.theSectionDate} {earlierSection.DateStr} {Lex.UnfinishedSection}");
        }

        // Setup TournamentRank Rank by Total KP
        var totalRank = 1;

        foreach (var team in teams.Where(t => t.Group == grp.Tournament.Title).OrderByDescending(t => t.TotalKP))
            team.TournamentRank = totalRank++;
    }

    private void buildpairs(BindableCollection<Pair> pairs, int grpNo, GroupSection grp)
    {
        bool InterWovenHowell = false;
        bool Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
        ImpsPair = grp.Tournament.TournamentPairCalcType == "2";

        if (grp.Resultlist is not null)
        {
            InterWovenHowell = grp.Tournament.MovementPlan?.Contains("Indvævet Howell") ?? false;

            foreach (var pair in grp.Resultlist.Pairs)
            {
                pair.GroupNo = grpNo;
                pair.Group = grp.Tournament.Title;

                pairs.Add(pair);
            }

            if (Mitchell)
            {
                HideHacGrp = false;
                var hacGrp = 1;

                foreach (var pair in pairs.Where(p => p.Direction == "1").OrderBy(p => p.HACRankSection))
                    pair.HACRankSectionGroup = hacGrp++;

                hacGrp = 1;

                foreach (var pair in pairs.Where(p => p.Direction == "2").OrderBy(p => p.HACRankSection))
                    pair.HACRankSectionGroup = hacGrp++;
            }
            else
                if (InterWovenHowell)
                {
                    HideHacGrp = false;
                    var subGroupSize = pairs.Count >> 1;

                    // Mark rank based on HAC achieved in this Section and Group
                    var hacGrp = 1;

                    foreach (var pair in pairs.Take(subGroupSize).OrderBy(p => p.HACRankSection))
                        pair.HACRankSectionGroup = hacGrp++;

                    hacGrp = 1;

                    foreach (var pair in pairs.Skip(subGroupSize).OrderBy(p => p.HACRankSection))
                        pair.HACRankSectionGroup = hacGrp++;
                }
        }

        if (grp.Startlist is not null)
            foreach (var pair in grp.Startlist.Pairs)
            {
                var res = pairs.FirstOrDefault(p => p.Group == grp.Tournament.Title && p.PairNo == pair.PairNo);

                if (res is null)
                {
                    pair.GroupNo = grpNo;
                    pair.Group = grp.Tournament.Title;
                    pairs.Add(pair);
                }
                else
                    res.StartPos = pair.StartPos;
            }

        if (InterWovenHowell)
        {
            ShowAsOneGroup = false;
            ShowAsOneGroupVisibility = Visibility.Visible;
        }
    }

    #region Load and Reload MainClub(s)
    private void loadMainClubs()
    {
        if (!Configuration.ReadBC3)
            return;

        if (string.IsNullOrWhiteSpace(Configuration.HomePagePath) || !Directory.Exists(Configuration.HomePagePath))
        {
            showMessageAFewSeconds($"{Lex.Folder}: '{Configuration.HomePagePath}' {Lex.DoNotExist}");
            _watcher.EnableRaisingEvents = Configuration.ReadBC3;
            return;
        }

        Logger.Info($"Loading all Main.xml files in {Configuration.HomePagePath}");

        foreach (var path in Directory.GetDirectories(Configuration.HomePagePath)
                                      .Select(dir => Path.GetFileName(dir))
                                      .Where(name => name.StartsWith("Resultater_", StringComparison.OrdinalIgnoreCase)))
        {
            var mainClub = loadMainClub(path);

            if (mainClub is not null && !MainClubs.Any(m => m.No == mainClub.No))
                MainClubs.Add(mainClub);
        }

        if (MainClubs.Count == 0)
        {
            showMessageAFewSeconds($"{Lex.MissingStartlist}: {Configuration.HomePagePath}");
            return;
        }

        MainClubs = MainClubs.OrderBy(mc => mc.Name).ToObservableCollection();

        if (_selectedMainClub is null)
        {
#if TEST
                    SelectedMainClub = MainClubs.FirstOrDefault(c => c.No == 9999) ?? MainClubs.First();
                    //SelectedPlayingTime = PlayingTimes.FirstOrDefault(p => p.DateStr.StartsWith("02-03-2026"));
#else
            SelectedMainClub = MainClubs.FirstOrDefault(c => c.No != 9999) ?? MainClubs.First();
#endif

            _watcher.EnableRaisingEvents = Configuration.ReadBC3;
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
        var path     = $@"{Configuration.HomePagePath}Resultater_{no}\";
        var filename = $@"{path}Main.xml";

        try
        {
            var mainclub = deserialize<MainClub>(filename);

            if (mainclub?.Clubs is null)
                return null;

            mainclub.Path = path;
            mainclub.No = no;

            return mainclub;
        }
        catch (Exception)
        {
            _lexStrings.Set(ErrorMessage, () => Lex.ErrorMainXml);
            return null;
        }
    }

    private void reloadSelectedClub(string mainPath)
    {
        try
        {
            var mainNew  = loadMainClub(mainPath);
            var mainClub = MainClubs.FirstOrDefault(m => m.No == mainNew.No);

            if (mainClub is null)
            {
                Logger.Info($"New mainclub read: {mainNew.Name}");
                // a new main club
                Execute.OnUIThread(
                    () =>
                    {
                        MainClubs.Add(mainNew);
                        showBadge(msg => MainClubBadge = msg, Lex.New);
                    });
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
                            Execute.OnUIThread(
                                () =>
                                {
                                    mainClub.Clubs.Add(clubNew);
                                    if (mainClub == SelectedMainClub)
                                        Clubs.Add(clubNew);
                                    showBadge(msg => SubClubBadge = msg, Lex.New);
                                });
                        else
                            for (var i = 0; i < mainClub.Clubs.Count; i++)
                            {
                                // Ordered insert
                                if (string.Compare(mainClub.Clubs[i].Name, clubNew.Name, StringComparison.CurrentCulture) <=
                                    0)
                                {
                                    if (mainClub == SelectedMainClub)
                                        Clubs.Insert(i, clubNew);

                                    Execute.OnUIThread(
                                        () =>
                                        {
                                            mainClub.Clubs.Insert(i, clubNew);
                                            showBadge(msg => SubClubBadge = msg, Lex.New);
                                        });

                                    break;
                                }
                            }
                    }
                    else
                        if (mainNew.No == mainClub.No && clubNew.Id == clubOld.Id)
                        {
                            Logger.Info($"Loading new tournaments for: {clubNew.Name}");

                            foreach (var mt in clubNew.MainTournaments)
                            {
                                var mtOld = clubOld.MainTournaments.FirstOrDefault(m => m.Name == mt.Name && m.Id == mt.Id);

                                if (mtOld is null)
                                {
                                    Execute.OnUIThread(
                                        () =>
                                        {
                                            clubOld.MainTournaments.Add(mt);
                                            foreach (var pt in mt.PlayingTimes)
                                                addToPlayingTimes(mt, pt);
                                        });

                                    continue;
                                }

                                foreach (var playingTimeNew in mt.PlayingTimes)
                                {
                                    var playingTimeOld = mtOld.PlayingTimes
                                                                      .FirstOrDefault(pt => pt.Date == playingTimeNew.Date);

                                    if (playingTimeOld is null)
                                        Execute.OnUIThread(() => addToPlayingTimes(mt, playingTimeNew));
                                    else
                                        if (playingTimeOld.Date == playingTimeNew.Date)
                                        {
                                            foreach (var fileNew in playingTimeNew.TournamentFiles)
                                            {
                                                var fileOld = playingTimeOld.TournamentFiles
                                                                                    .FirstOrDefault(o => o.GroupName == fileNew.GroupName && o.Id == fileNew.Id);

                                                if (fileOld is null)
                                                {
                                                    Execute.OnUIThread(
                                                        () =>
                                                        {
                                                            playingTimeOld.TournamentFiles.Add(fileNew);
                                                            PlayingTimes.Add(playingTimeNew);
                                                            PlayingTimes = new BindableCollection<PlayingTime>(
                                                                                PlayingTimes.OrderByDescending(s => s.Date));
                                                            SelectedPlayingTime = null;
                                                            SelectedPlayingTime = playingTimeOld;
                                                        });

                                                    continue;
                                                }
                                                else
                                                    Execute.OnUIThread(
                                                        () =>
                                                        {
                                                            fileOld.Merge(fileNew);
                                                        });
                                            }

                                            if (SelectedPlayingTime.Date == playingTimeNew.Date)
                                                fetchPlayingTime();
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
            _lexStrings.Set(ErrorMessage, () => Lex.ErrorMainXml);
            Logger.Exception(ex, ErrorMessage.Value);
        }
    }

    private void addToPlayingTimes(MainTournament mt, PlayingTime pt)
    {
        pt.MainTournament = mt;

        var cnt = PlayingTimes.Count;

        for (var i = 0; i < cnt; i++)
        {
            if (PlayingTimes[i].Date < pt.Date)
            {
                //Execute.OnUIThread(() => PlayingTimes.Insert(i, pt));
                PlayingTimes.Insert(i, pt);

                showBadge(msg => DateBadge = msg, Lex.New);

                return; // break;
            }
        }

        Execute.OnUIThread(() => PlayingTimes.Add(pt));
    }
    #endregion

    private void initSubgroups(BindableCollection<Pair> pairs = null)
    {
        pairs ??= Pairs;

        for (var grpNo = 0; grpNo < GroupSections.Count; grpNo++)
        {
            var grp = GroupSections[grpNo];

            if (grp.Tournament.TournamentType.Text == "Parturnering" && grp.Resultlist is not null)
            {
                var InterwovenHowell = grp.Tournament.MovementPlan?.Contains("Indvævet Howell") ?? false;
                var Mitchell         = grp.Tournament.MovementPlanType == MovementPlans.Mitchell;
                var subGroupSize     = grp.Resultlist.Pairs.Count >> 1;
                var rankA            = 1;
                var rankB            = 1;

                foreach (var pair in pairs.Where(p => p.GroupNo == grpNo).OrderBy(p => p.SectionRank))
                {
                    pair.Position = pair.Rank;
                    pair.Group = grp.Tournament.Title;

                    if (InterwovenHowell)
                    {
                        pair.SubGroup = pair.PairNo <= subGroupSize ? Lex.FirstHalf : Lex.SecondHalf;
                        pair.Position = pair.PairNo <= subGroupSize ? rankA++ : rankB++;
                    }
                    else
                        if (Mitchell)
                        {
                            pair.SubGroup = pair.Direction == "2" ? Lex.EW : Lex.NS;
                            pair.Position = pair.Rank;
                        }
                        else
                        {
                            pair.SubGroup = string.Empty;
                            pair.Position = pair.Rank;
                        }
                }
            }
        }
    }

    #region Get XML data
    private List<Tournament> getTournaments(PlayingTime pt)
    {
        if (_watcher is not null)
            foreach (var pathName in _watcher.Filters.Where(f => f.StartsWith("MT") || f.StartsWith("GT")).ToList())
                _watcher.Filters.Remove(pathName);

        List<Tournament> tournaments = new();

        if (pt is null || pt.TournamentFiles is null)
            _lexStrings.Set(ErrorMessage, () => Lex.BC3NotUploaded);
        else
            foreach (var tournamentFile in pt.TournamentFiles)
            {
                if (string.IsNullOrEmpty(tournamentFile.FileName))
                {
                    if (ErrorMessage.Value.StartsWith(Lex.BC3Data))
                        _lexStrings.Set(ErrorMessage, () => Lex.BC3NotUploaded);
                    else
                        _lexStrings.Set(
                                         ErrorMessage
                                       , () => $"{Lex.BC3Data} '{tournamentFile.GroupName}' {Lex.NotUploaded}");

                    continue;
                }

                var path       = $"{SelectedMainClub.Path}{tournamentFile.FileName}";
                var tournament = deserialize<Tournament>(path);

                _watcher.Filters.Add(tournamentFile.FileName);

                if (tournament is null)
                    if (string.IsNullOrEmpty(ErrorMessage.Value))
                        _lexStrings.Set(
                                         ErrorMessage
                                       , () => $"{Lex.BC3DataFor} '{tournamentFile.GroupName}' {Lex.NotUploaded}");
                    else
                        _lexStrings.Set(ErrorMessage, () => Lex.BC3NotUploaded);
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
        SectionNo = 1;

        foreach (var pathName in _watcher.Filters.Where(f => f.StartsWith("GT")).ToList())
            _watcher.Filters.Remove(pathName);

        foreach (var tur in tournaments)
        {
            var path    = $"{SelectedMainClub.Path}{tur.SectionFile.FileName}";
            var section = deserialize<GroupSection>(path);

            _watcher.Filters.Add(tur.SectionFile.FileName);

            if (section != null)
            {
                section.Tournament = tur;
                sections.Add(section);

                if (tur.SectionNo > SectionNo)
                    SectionNo = tur.SectionNo;
            }
        }

        return sections;
    }

    private GroupSection getGroupSection(string fileName, Tournament tournament)
    {
        var path    = $"{SelectedMainClub.Path}{fileName}";
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
                string json = readAllTextWithRetry(fullPath, _iso_8859_1);
                return JsonSerializer.Deserialize<T>(json, _jsonOptions);
            }
            else // XML
            {
                // Return nulle when the file doesn't exsist
                string xml = readAllTextWithRetry(fullPath, _iso_8859_1);

                // Remove tags only containing blanks and hyphens
                xml = Regex.Replace(xml, @">(-|\s)+<", "><");

                // Replace commas with dots in decimalnumbers like (fx 123,45 -> 123.45) 
                xml = Regex.Replace(xml, @"(?<=\d),(?=\d)", ".");

                // Remove empty tags
                xml = Regex.Replace(xml, @"<(\w+)(\s[^>]*)?>\s*</\1>", string.Empty);       // Remove <TagName></TagName>
                xml = Regex.Replace(xml, @"<[A-Za-z_][A-Za-z0-9_.:-]*\s*/>", string.Empty); // Removes self-closing tags like <Tag/>

                var       serializer = new XmlSerializer(typeof(T));
                using var reader     = new StringReader(xml);
                return (T)serializer.Deserialize(reader);
            }
        }
        catch (Exception)
        {
            Logger.Info($"{Lex.ErrorDeserializing}: {fullPath}");
            _lexStrings.Set(ErrorMessage, () => Lex.ErrorReadingStartOrResultLists);
            return default;
        }
    }

    private string readAllTextWithRetry(string path, Encoding encoding, int maxAttempts = 10, int initialDelayMs = 100)
    {
        var delay = initialDelayMs;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
            try
            {
                // Open in ReadWrite mode with sharing (if allowed).
                using var fs = new FileStream(
                                                       path
                                                     , FileMode.Open
                                                     , FileAccess.Read
                                                     , FileShare.ReadWrite | FileShare.Delete);
                using var sr = new StreamReader(fs, encoding);
                return sr.ReadToEnd();
            }
            catch (IOException) when (attempt < maxAttempts)
            {
                Thread.Sleep(delay);
                delay = Math.Min(1000, delay * 2); // exponential backoff, cap at 1s
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts)
            {
                Thread.Sleep(delay);
                delay = Math.Min(1000, delay * 2);
            }

        // Last attempt (lets exception bubble up if it fails)
        using var fsFinal = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var srFinal = new StreamReader(fsFinal, encoding);
        return srFinal.ReadToEnd();
    }
    #endregion

    private async Task showProjector()
    {
        var           projectorScreen = WpfScreenHelper.Screen.AllScreens
                                                                  .Where(s => !s.Primary)
                                                                  .OrderByDescending(s => s.Bounds.Width * s.Bounds.Height)
                                                                  .FirstOrDefault();
        ProjectorView projectorView   = null;

        if (projectorScreen is null)
        {
            //#if RELEASE
#if RELEASE
            MessageBox.Show("Der er ikke oprettet forbindelse til en sekundær skærm. Tast Win+K", "Info");
#else
                var primaryScreen = WpfScreenHelper.Screen.PrimaryScreen;

                projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                if (projectorView is null)
                {
                    await _windowManager.ShowWindowAsync(this, "ProjectorView").ConfigureAwait(false);

                    projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

                    projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
                    projectorView.WindowState           = WindowState.Normal;
                    projectorView.Width                 = 800;
                    projectorView.Height                = 600;

                    projectorView.Top  = primaryScreen.WorkingArea.Top;
                    projectorView.Left = primaryScreen.WpfBounds.Left + primaryScreen.WpfBounds.Width - projectorView.Width;
                }
#endif
        }
        else

        {
            projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();

            if (projectorView is null)
            {
                await _windowManager.ShowWindowAsync(this, "ProjectorView").ConfigureAwait(false);
                projectorView = Application.Current.Windows.OfType<ProjectorView>().FirstOrDefault();
            }

            projectorView.WindowStartupLocation = WindowStartupLocation.Manual;
            projectorView.Left = projectorScreen.WorkingArea.Left;
            projectorView.Top = projectorScreen.WorkingArea.Top;
            projectorView.Width = projectorScreen.WorkingArea.Width;
            projectorView.Height = projectorScreen.WorkingArea.Height;
            projectorView.WindowState = WindowState.Maximized;
        }

        if (projectorView == null)
            Logger.Error("Could not find or create ProjectorView");
        else
            Logger.Info($"ProjectorView shown on {(projectorScreen != null ? $"screen: {projectorScreen.DeviceName}" : "primary screen")}");

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

    private void pairsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    { NotifyOfPropertyChange(() => Pairs); }
    private void teamsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    { NotifyOfPropertyChange(() => Teams); }
    private void configurationPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Configuration.CultureName))
            _lexStrings.RefreshAll();
    }

    #region FileWatcher and Handlers
    private async Task handleFileEventAsync(FileSystemEventArgs ev)
    {
        if (_watcher?.EnableRaisingEvents == true)
        {
            Logger.Debug($"File Event: {ev.FullPath}");
            try
            {
                if (ev.FullPath.StartsWith(Configuration.HomePagePath))
                    reloadSelectedClub(ev.FullPath);
                else
                    initWatcher();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling event on UI thread: {ex.Message}");
                _lexStrings.Set(ErrorMessage, () => Lex.ErrorReadingStartOrResultLists);
            }
        }
    }

    internal void SetBC3Watcher(bool enable)
    {
        if (enable)
            loadMainClubs();
        else
            SelectedMainClub = null;

        _watcher.EnableRaisingEvents = Configuration.ReadBC3;
    }

    internal void SetBridgeMateWatcher(bool enable)
    {
        if (enable)
            BridgeMate.CheckOrOpen(SelectedPlayingTime?.Date, SelectedMainClub?.No);
        else
            BridgeMate.Close();
    }
    #endregion

    #region Handle BridgeMate RoundStatus changes
    private void roundStatusItemChanged(object sender, ItemPropertyChangedEventArgs<RoundStatus> e)
    {
        if (e.PropertyName == nameof(RoundStatus.Done))
            checkRoundStatus(e.Item);
    }

    private void checkRoundStatus(RoundStatus roundStatus)
    {
        List<BridgeTimer> timers = Configuration.GetRelatedTimers(roundStatus, _threshold);

        foreach (var timer in timers)
            if (timer.Round > roundStatus.Round)
                timer.Round = roundStatus.Round;

        if (roundStatus.Done)
        {
            Logger.Info($"Round done: {roundStatus}");

            foreach (var timer in timers)
            {
                if (BridgeMate.RoundStatus
                              .Where(s => s.Round == roundStatus.Round && s.Letter == roundStatus.Letter)
                              .All(s => s.Done))
                    if (timer.RemainingTime > TimeSpan.Zero)
                        timer.FinishRound((int)roundStatus.Round);
            }
        }
    }
    #endregion

    #region Overrides
    protected override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        Logger.Info("Activating ControlViewModel");
        return base.OnActivateAsync(cancellationToken);
    }

    // IDisposable pattern
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        _watcher?.EnableRaisingEvents = false;

        if (_disposed)
            return;

        _disposed = true;

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
            if (_watcher is not null)
            {
                _watcher.UpdatedAsync -= handleFileEventAsync;
                _watcher.Dispose();
                _watcher = null;
            }
        }
    }

    ~ControlViewModel() { Dispose(false); }
    #endregion
    #endregion
}
