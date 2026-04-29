using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AppArguments;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.Helpers;
using DBF.Resources;
using DBF.ViewModels;
using Localization;
using Syncfusion.Data.Extensions;
using Syncfusion.DocIO.DLS;
using Syncfusion.UI.Xaml.ImageEditor;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.DataModel
{
    public partial class Configuration : PropertyChangedBase
    {
        private static readonly string[]              audioExtensions   = new[] {                    ".wav",               ".mp3" };
        private static readonly JsonSerializerOptions serializerOptions = new JsonSerializerOptions {WriteIndented = true, };
        private static          string                currentversion    = "v" + Assembly.GetExecutingAssembly().GetName().Version;
        private static          string                path              = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mortensp\\DBF\\configuration.json";
        private static          string                oldPath           = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DBFTools\\configuration.json";
        private                 Configuration         loadedConfig;
        private                 string                configVersion     = "v01";
        private                 int                   visibleTimerCount;
        private static readonly TimeSpan              _fiveHours        = new TimeSpan(5, 0, 0);
        private static readonly TimeSpan              _zeroTime         = new TimeSpan(0, 0, 0);
        private                 DateTime              startDate         = new(2026,1,1,18,30,0);

        private IWindowManager  windowManager = IoC.Get<IWindowManager>();
        private ICollectionView _presetsView;

        #region Constructors
            static Configuration()
            {
                serializerOptions.Converters.Add(new PresetCollectionConverter());

                // Make sure that Roaming folder exists
                var configDir = Path.GetDirectoryName(path);

                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);
            }

            public Configuration()
            {
                BridgeTimers.CollectionChanged+= bridgeTimers_CollectionChanged;
                Presets.CollectionChanged     += presets_CollectionChanged;
                Presets.ItemChanged           += presets_ItemChanged;
            }
        #endregion

        #region Public Properties
            #region Public Properties - Serilizable
                public static readonly string StatePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Mortensp\\DBF\\state.json";
                public string BC3Path
                {
                    get => field;
                    set
                    {
                        field = value.Trim();

                        if (string.IsNullOrWhiteSpace(field))
                            field = @"C:\BC3\";
                        else
                            if (!field.EndsWith('/'))
                                field += '/';
                    }
                } = @"C:\BC3\";

                public string AppVersion        { get; private set; } = currentversion;
                public string ConfigVersion     { get; set; }
                public bool   IsLoaded          { get; private set; }
                public bool   ReadBC3           { get; set; } = true;
                public bool   ReadBridgeMate    { get; set; } = false;
                public int    ProjectorInterval { get; set; } = 20;
                public int    ProjectorMaxRows  { get; set; } = 40;

                public string HomepagePath      => BC3Path + @"Hjemmeside\";
                public string BridgeMatePath    => BC3Path + @"BridgeMate\";
                public TimeOnly? StartTime
                {
                    get => field ?? TimeOnly.FromDateTime(startDate);

                    set
                    {
                        if (Set(ref field, value))
                            StartDate = new( startDate.Year
                                           , startDate.Month
                                           , startDate.Day
                                           , value?.Hour ?? 18
                                           , value?.Minute ?? 30
                                           , 0);
                    }
                }

                public BindableCollectionExt<Preset> Presets { get; set; } =
                            new() {     new Preset("Par - 7 runder af 4 spil",  false, false, 7,  4,  4, 0, 27, 0, 1, 12, 5)
                                      , new Preset("Par - 8 runder af 4 spil",  false, false, 8,  4,  4, 0, 27, 0, 1, 10, 5)
                                      , new Preset("Par - 9 runder af 3 spil",  false, false, 9,  3,  5, 0, 21, 0, 1, 12, 5)
                                      , new Preset("Par - 11 runder af 2 spil", false, false, 11, 2,  6, 0, 14, 0, 1, 12, 5)
                                      , new Preset("Hold kamp af 32 spil",      false, true,  2,  16, 1, 1, 46, 0, 0, 15, 5)
                                  };

                [JsonIgnore]
                public ICollectionView PresetsView
                {
                    get
                    {
                        if (_presetsView == null)
                        {
                            // Sørg for at dette kaldes fra UI-tråden
                            _presetsView        = CollectionViewSource.GetDefaultView(Presets);
                            _presetsView.Filter = o => o is Preset p && !p.IsHidden;

                            // Hook collection / item change handlers so view refreshes when items or their IsHidden flag change
                            Presets.CollectionChanged+= presets_CollectionChanged;
                            Presets.ItemChanged      += presets_ItemChanged;

                            // Ensure UI shows filtered list immediately
                            Application.Current?.Dispatcher?.Invoke(() => _presetsView.Refresh());
                        }

                        return _presetsView;
                    }
                }

                public ObservableCollection<BridgeTimer> BridgeTimers { get; set; } = new();

                public List<BridgeTimer> GetRelatedTimers(RoundStatus roundStatus, TimeSpan? threshold = null)
                {
                    TimeSpan timeLimit = threshold?? TimeSpan.Zero;

                    return BridgeTimers.Where(t =>  t.Visibility    == Visibility.Visible
                                                &&  t.RemainingTime >= timeLimit
                                                //&&  t.Round         <= roundStatus.Round
                                                &&  t.GroupList.Contains(roundStatus.Letter))
                                       .ToList();
                }

                public List<BridgeTimer> GetRelatedTimers(string sectionLetter, TimeSpan? threshold = null)
                {
                    TimeSpan timeLimit = threshold?? TimeSpan.Zero;

                    return BridgeTimers.Where(t =>  t.Visibility    == Visibility.Visible
                                                &&  t.RemainingTime >  timeLimit
                                                //&&  t.Round         <= round
                                                &&  t.GroupList.Contains(sectionLetter))
                                       .ToList();
                }
            #endregion

            #region Public Properties - JsonIgore
                [JsonIgnore]
                public DateTime StartDate
                {
                    get
                    {
                        var limit = startDate.AddMinutes(-30);
                        var now   = DateTime.Now;

                        if (now >= startDate
                        &&  now <= limit)
                            return now;

                        return startDate;
                    }

                    set
                    {
                        if (Set(ref startDate, value))
                            setEndTime();
                    }
                }

                [JsonIgnore]
                public TimeOnly EndTime          { get; set; }

                [JsonIgnore]
                public bool     TimersCanBeAdded { get; set; }

                [JsonIgnore]
                public int VisibleTimerCount
                {
                    get => visibleTimerCount;
                    set
                    {
                        visibleTimerCount = value;
                        TimersCanBeAdded  = VisibleTimerCount <  4;
                    }
                }

                [JsonIgnore]
                public ObservableCollection<CustomColor> BackgroundColors = 
                                                         new() {     new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FFFFFF"),   ColorName = "Hvid" }
                                                                   , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#F2460D"),   ColorName = "Rød (dbf)" }
                                                                   , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FF66CCFF"), ColorName = "Blå (dbf)" }
                                                                   , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#FF9D00"),   ColorName = "Orange (dbf)" }
                                                                   , new CustomColor() {Color = (Color)ColorConverter.ConvertFromString("#81C784"),   ColorName = "Grøn (dbf)" }
                                                               };

                [JsonIgnore]
                public bool TimersActive
                {
                    get
                    {
                        for (int i = 0; i <  VisibleTimerCount; i++)
                            if (BridgeTimers[i].IsActive)
                                return true;

                        return false;
                    }
                }
            #endregion
        #endregion

        #region Public Methods
            #region Save / Load Configuration
                public void Save()
                {
                    ConfigVersion = configVersion;

                    foreach (var preset in Presets.Where(p => p.CustomPreset == false))
                        preset.IsHidden = Presets.Any(p => p.CustomPreset == true && p.Name == preset.Name);

                    // Only Custom Presets are saved due to PresetCollectionConverter
                    string json = JsonSerializer.Serialize(this, serializerOptions);
                    File.WriteAllText(path, json);

                    setEndTime();
                }

                public async Task LoadAsync()
                {
                    if (!File.Exists(path)
                    &&  File.Exists(oldPath))
                    {
                        // Sørg for at destination-mappen eksisterer
                        Directory.CreateDirectory(Path.GetDirectoryName(path));

                        // Flyt gammel config fil
                        File.Move(oldPath, path);

                        // Slet evt. gammel tom mappe
                        string sourceFolder = Path.GetDirectoryName(oldPath)!;

                        if (Directory.GetFiles(sourceFolder).Length       == 0
                        &&  Directory.GetDirectories(sourceFolder).Length == 0)
                            Directory.Delete(sourceFolder);
                    }

                    //var shell = IoC.Get<ShellViewModel>();
                    if (!File.Exists(path)
                    ||  Arguments.Values.Lookup("mode") == "reset")
                    {
                        AppVersion    = currentversion;
                        ConfigVersion = configVersion;
                        Save();
                        await OpenSettingsAsync();
                    }
                    else
                    {
                        var jsonData = File.ReadAllText(path);

                        if (string.IsNullOrWhiteSpace(jsonData))
                        {
                            AppVersion = currentversion;
                            await OpenSettingsAsync();
                            Save();
                        }
                        else
                        {
                            Logger.Info("Reading Configuration file");

                            //TODO: kan fjernes senere
                            if (jsonData.IndexOf("\"Color\":") >  -1)
                                jsonData = jsonData.Replace("\"Color\":", "\"BackgroundColor\":");

                            loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonData, serializerOptions);

                            Update(loadedConfig);

                            if (loadedConfig.ConfigVersion                          is null
                            ||  loadedConfig.ConfigVersion.CompareTo(configVersion) <  0)
                                await OpenSettingsAsync();
                        }
                    }

                    loadTimers();
                    IsLoaded = true;
                }

                private void loadTimers()
                {
                    int i;
                    BridgeTimers.Clear();

                    for (i = 0; i <  4; i++)
                    {
                        BridgeTimer timer;

                        if (loadedConfig?.BridgeTimers      is null
                        ||  loadedConfig.BridgeTimers.Count == 0)
                        {
                            timer = new BridgeTimer();
                            timer.Update(Presets[i]);
                            timer.Name            = null;
                            timer.BackgroundColor = BackgroundColors[i].Color;
                            timer.Groups          = (GroupFlags)(1 << i); // Set group to A, B, C or D

                            if (visibleTimerCount >  1) // Lad som standard de to første være visible
                                timer.Visibility = System.Windows.Visibility.Collapsed;
                        }
                        else
                            if (loadedConfig.BridgeTimers.Count >  i)
                                timer = loadedConfig.BridgeTimers[i];
                            else
                            {
                                timer = new();
                                timer.Update(Presets[i]);
                                timer.Visibility = System.Windows.Visibility.Collapsed;
                            }

                        if (timer.BreakAfterRound == 0
                        ||  timer.BreakMinutes    == 0)
                            timer.BreakAfterRound = timer.BreakMinutes = 0;

                        if (string.IsNullOrEmpty(timer.Sound))
                            timer.Sound = AudioResources.Sounds[i];

                        BridgeTimers.Add(timer);

                        if (timer.Visibility == Visibility.Visible)
                        {
                            timer.UpdateDisplay();
                            VisibleTimerCount++;
                        }
                    }

                    for (; i <  4; i++)
                    {
                        var timer = new BridgeTimer();
                        timer.Update(Presets[i]);
                        timer.Name            = null;
                        timer.BackgroundColor = BackgroundColors[i].Color;
                        timer.Groups          = (GroupFlags)(1 << i); // Set group to A, B, C or D
                        timer.Visibility      = Visibility.Collapsed;
                        timer.Sound           = AudioResources.Sounds[i];

                        BridgeTimers.Add(timer);

                        if (timer.Visibility == Visibility.Visible)
                            VisibleTimerCount++;
                    }

                    if (visibleTimerCount == 0)
                    {
                        BridgeTimers[0].Visibility = Visibility.Visible;
                        BridgeTimers[0].UpdateDisplay();
                        visibleTimerCount = 1;
                        Save();
                    }

                    arrangeTimers();
                    SetUpDownVisibility();
                    RestoreState();
                }
            #endregion

            private void arrangeTimers()
            {
                // Move collapsed timers at the end and enable/diable Close buttons
                for (var i = BridgeTimers.Count - 1; i >= 0; i--)
                {
                    var timer = BridgeTimers[i];

                    if (timer.Visibility != Visibility.Visible)
                        BridgeTimers.Move(i, BridgeTimers.Count - 1);
                    else
                        timer.CanClose = visibleTimerCount >  1;
                }
            }

            #region State Serializing 
                public void DeleteState()
                {
                    if (File.Exists(StatePath))
                        File.Delete(StatePath);
                }

                public void SaveState()
                {
                    var controlViewModel = IoC.Get<ControlViewModel>();
                    var state            = new State()
                                           {
                                               MainClubName   = controlViewModel.SelectedMainClub?.Name
                                             , SubClubName    = controlViewModel.SelectedClub?.Name
                                             , PlayingTimeStr = controlViewModel.SelectedPlayingTime?.DateStr
                                             , TimerStates    = BridgeTimers.Where(t=>t.IsStarted).Select(t => t.CurrentState).ToList()
                                           };

                    //var states = BridgeTimers.Select(t => t.CurrentState)
                    //                             .ToList();
                    string json = JsonSerializer.Serialize(state, serializerOptions);
                    File.WriteAllText(StatePath, json);
                }

                public void RestoreState()
                {
                    //if (Arguments.Values.Lookup("mode") == "restart")
                {
                    if (File.Exists(StatePath))
                        try
                        {
                            Logger.Info("Restoring Timers");
                            ; //todo: Empty Statement!! 
                            var jsonData = File.ReadAllText(StatePath);
                            var state    = JsonSerializer.Deserialize<State>(jsonData, serializerOptions);

                            var controlViewModel = IoC.Get<ControlViewModel>();

                            if (state.MainClubName != null)
                                controlViewModel.SelectedMainClub = controlViewModel.MainClubs.FirstOrDefault(mc => mc.Name == state.MainClubName);

                            if (state.SubClubName != null)
                                controlViewModel.SelectedClub = controlViewModel.Clubs.FirstOrDefault(mc => mc.Name == state.SubClubName);

                            if (string.IsNullOrWhiteSpace(state.PlayingTimeStr) == false)
                                controlViewModel.SelectedPlayingTime = controlViewModel.PlayingTimes.FirstOrDefault(mc => mc.DateStr == state.PlayingTimeStr);

                            for (int i = 0; i <  state.TimerStates.Count && i <  state.TimerStates.Count; i++)
                                if (state.TimerStates[i].IsStarted)
                                    BridgeTimers[i].Restore(state.TimerStates[i]);
                        }
                        catch (Exception ex)
                        {
                            Logger.Exception(ex, "Fejl ved genskabelse af state");
                            Debugger.Break();
                        }

                        finally { /* Ignore */ }
                }
                }
            #endregion

            public async Task OpenSettingsAsync()
            {
                var viewModel = IoC.Get<ConfigurationViewModel>();
                await windowManager.ShowDialogAsync(viewModel);
            }

    

            public void OpenJSONFiles()
            {
                tryOpenFile(path);

                SaveState();
                tryOpenFile(StatePath);
            }

            public void OpenLogFile()
            {
                tryOpenFile(Logger.LogFilePath);
            }

            public void Update(Configuration newConfiguration)
            {
                ReadBridgeMate    = newConfiguration.ReadBridgeMate;
                ReadBC3           = newConfiguration.ReadBC3;
                BC3Path           = newConfiguration.BC3Path;
                ProjectorInterval = newConfiguration.ProjectorInterval;
                ProjectorMaxRows  = newConfiguration.ProjectorMaxRows;
                StartTime         = newConfiguration.StartTime;

                if (newConfiguration.hasUserValues)
                {
                    ProjectorInterval = newConfiguration.ProjectorInterval;
                    ProjectorMaxRows  = newConfiguration.ProjectorMaxRows;
                }

                // Keep built-in Presets
                foreach (var preset in newConfiguration.Presets.Where(p => p.CustomPreset))
                {
                    var buitlin = Presets.FirstOrDefault(p => p.Name == preset.Name && p.CustomPreset==false);

                    if (buitlin is not null)
                        buitlin.IsHidden = true;

                    preset.CustomPreset = true;
                    preset.IsHidden     = false;

                    Presets.Add(preset);
                }
            }
        #endregion

        #region Internal Timer Management Methods   
            internal void AddTimer()
            {
                if (VisibleTimerCount <  4)
                {
                    var bridgeTimer = BridgeTimers[VisibleTimerCount];

                    bridgeTimer.Visibility = Visibility.Visible;

                    bridgeTimer.UpdateDisplay();
                    VisibleTimerCount++;
                    arrangeTimers();
                    Save();
                    SetUpDownVisibility();
                }
            }

            internal void CloseTimer(BridgeTimer timer)
            {
                if (!timer.IsStarted
                ||  MessageBoxResult.OK == MessageBox.Show( $"{Lex.ThisWillResetTheTimer} {Lex.ContinueQuestion}?"
                                                          , Lex.Confirmation
                                                          , MessageBoxButton.OKCancel
                                                          , MessageBoxImage.Question))
                {
                    VisibleTimerCount--;
                    timer.Reset(false);
                    timer.Visibility = Visibility.Collapsed;

                    arrangeTimers();
                    Save();
                    SetUpDownVisibility();
                }
            }

            internal void TimerUp(BridgeTimer timer)
            {
                if (timer.Visibility == Visibility.Visible)
                {
                    var i               = BridgeTimers.IndexOf(timer);
                    var gem             = BridgeTimers[i - 1];
                    BridgeTimers[i - 1] = timer;
                    BridgeTimers[i]     = gem;

                    Save();
                    SetUpDownVisibility();
                }
            }

            internal void TimerDown(BridgeTimer timer)
            {
                if (timer.Visibility == Visibility.Visible)
                {
                    var i               = BridgeTimers.IndexOf(timer);
                    var gem             = BridgeTimers[i + 1];
                    BridgeTimers[i + 1] = timer;
                    BridgeTimers[i]     = gem;

                    Save();
                    SetUpDownVisibility();
                }
            }
        #endregion

        #region Private Methods
            private void tryOpenFile(string path)
            {
                var psi = new ProcessStartInfo
                          {
                              Arguments        = $"\"{path}\""
                            , WorkingDirectory = Path.GetDirectoryName(path)
                            , UseShellExecute  = true
                          };

                if (File.Exists(path))
                    try
                    {
                        psi.FileName = "notepad++.exe";
                        Process.Start(psi);
                    }

                    catch
                    {
                        psi.FileName = "notepad.exe";
                        Process.Start(psi);
                    }
            }

            private bool hasUserValues
            {
                get
                {
                    if (AppVersion.CompareTo("v0.9.3.0") <= 0)
                        return !(ProjectorInterval == 20 && ProjectorMaxRows == 40);
                    else
                        return false;
                }
            }

            private void setEndTime()
            {
                var active = BridgeTimers.Where(t => t.Visibility == Visibility.Visible);

                if (active.Count() == 0)
                    EndTime = new();
                else
                    EndTime = active.Max(t => t.EndTime ?? TimeOnly.MinValue);
            }

            private void SetUpDownVisibility()
            {
                foreach (var timer in BridgeTimers)
                {
                    timer.ShowUpButton   = Visibility.Visible;
                    timer.ShowDownButton = Visibility.Visible;
                }

                BridgeTimers[0].ShowUpButton                       = Visibility.Collapsed;
                BridgeTimers[VisibleTimerCount - 1].ShowDownButton = Visibility.Collapsed;
            }
        #endregion

        #region private BridgeTimer Collection Change Handling
            private void bridgeTimers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.NewItems != null)
                    foreach (BridgeTimer timer in e.NewItems)
                        timer.PropertyChanged += BridgeTimer_PropertyChanged;

                if (e.OldItems != null)
                    foreach (BridgeTimer timer in e.OldItems)
                        timer.PropertyChanged -= BridgeTimer_PropertyChanged;
            }

            private void BridgeTimer_PropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(BridgeTimer.EndTime))
                    setEndTime();
            }
        #endregion

        internal void StartAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.Start();
        }

        internal void PauseAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.Pause();
        }

        internal void MoreTimeAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.MoreTime();
        }

        internal void LessTimeAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.LessTime();
        }

        internal void ForwardAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.Forward();
        }

        internal void BackAll()
        {
            foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                timer.Back();
        }

        internal void ResetAll()
        {
            var timers = BridgeTimers.Where(t => t.Visibility == Visibility.Visible);
            var first  = true;
            var plural = timers.Count()>1;

            foreach (var timer in timers)
            {
                timer.Reset(first, plural);
                first = false;
            }
        }

        #region Handel PresetsView 
            private void presets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                _presetsView?.Refresh();
            }

            private void presets_ItemChanged(object sender, ItemPropertyChangedEventArgs<Preset> e)
            {
                _presetsView?.Refresh();
            }
        #endregion
    }
}

