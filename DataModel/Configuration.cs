using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Data;

using AppArguments;

using Caliburn.Micro;

using DBF.AudioServices;
using DBF.Helpers;
using DBF.ViewModels;

using String.Localization;

using Syncfusion.Windows.Tools.Controls;

namespace DBF.DataModel
{
    public partial class Configuration : PropertyChangedBase
    {
        #region Private Fields & Properties
            private static readonly JsonSerializerOptions _serializerOptions = new JsonSerializerOptions {WriteIndented = true, };
            private static          string                _currentversion    = "v" + Assembly.GetExecutingAssembly().GetName().Version;
            private static          string                _path              = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mortensp\\DBF\\configuration.json";
            private static          string                _oldPath           = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DBFTools\\configuration.json";
            private                 Configuration         _loadedConfig;
            private                 string                _configVersion     = "v01";
            private                 int                   _visibleTimerCount;
            private                 DateTime              _startDate         = new(2026,1,1,18,30,0);
            private                 string                _cultureName;
            private                 IWindowManager        _windowManager     = IoC.Get<IWindowManager>();
            private                 ICollectionView       _presetsView;
        #endregion

        #region Constructors
            static Configuration()
            {
                _serializerOptions.Converters.Add(new PresetCollectionConverter());

                // Make sure that Roaming folder exists
                var configDir = Path.GetDirectoryName(_path);

                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);
            }

            public Configuration()
            {
                BridgeTimers.CollectionChanged+= bridgeTimers_CollectionChanged;
                Presets.CollectionChanged     += presets_CollectionChanged;
                Presets.ItemChanged           += presets_ItemChanged;

                CultureName = Arguments.Values?.Lookup("language");
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

                public string      AppVersion            { get; private set; } = _currentversion;
                public string      ConfigVersion         { get; set; }
                public bool        IsLoaded              { get; private set; }
                public bool        ReadBC3               { get; set; } = true;
                public bool        ReadBridgeMate        { get; set; } = false;
                public int         ProjectorInterval     { get; set; } = 20;
                public int         ProjectorMaxRows      { get; set; } = 40;
                public string      HomePagePath          => BC3Path + @"Hjemmeside\";
                public string      BridgeMatePath        => BC3Path + @"BridgeMate\";

                public Orientation WindowOrientation     { get; set; } = Orientation.Horizontal;

                public string      WindowOrientationIcon => VisibleTimerCount == 1
                                                          ? null
                                                          : WindowOrientation == Orientation.Horizontal
                                                          ? "/Images/VerticalWindows.PNG"
                                                          : "/Images/HorizontalWindows.PNG";
                public TimeOnly? StartTime
                {
                    get => field ?? TimeOnly.FromDateTime(_startDate);

                    set
                    {
                        if (Set(ref field, value))
                            StartDate = new( _startDate.Year
                                           , _startDate.Month
                                           , _startDate.Day
                                           , value?.Hour ?? 18
                                           , value?.Minute ?? 30
                                           , 0);
                    }
                }

                public BindableCollectionExt<Preset> Presets { get; set; } = new();

                #region Culture and CultureName Properties
                    public static BindableCollection<CultureInfo> UILanguages
                    {
                        get
                        {
                            if (field is null)
                            {
                                var languages = LanguageService.Instance.GetAvailableCultures("en-US").ToList();

                                field = new BindableCollection<CultureInfo>(languages.OrderBy(c => c.DisplayName));
                            }

                            return field;
                        }
                    }

                    public string CultureName
                    {
                        get => _cultureName;
                        set
                        {
                            if (string.IsNullOrWhiteSpace(value))
                                value = "en-US";

                            if (Set(ref _cultureName, value?.Trim()))
                                if (Culture?.Name != value)
                                    Culture = UILanguages.FirstOrDefault(c => c.Name == value)
                                           ?? new CultureInfo(value);
                        }
                    }

                    [JsonIgnore]
                    public CultureInfo Culture
                    {
                        get => field;
                        set
                        {
                            if (value?.Name != field?.Name)
                            {
                                field = UILanguages.FirstOrDefault(c => c.Name == value.Name);

                                if (field == null)
                                    if (string.IsNullOrWhiteSpace(value.Name))
                                        new CultureInfo("en-US");
                                    else
                                        new CultureInfo(value.Name);

                                if (CultureName != value?.Name)
                                    CultureName =  value?.Name;
                            }
                        }
                    }
                #endregion

                [JsonIgnore]
                public ICollectionView PresetsView
                {
                    get
                    {
                        if (_presetsView == null)
                        {
                            // Make sure this is called from the UI thread
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

                public BindableCollectionExt<BridgeTimer> BridgeTimers { get; set; } = new();

                public List<BridgeTimer> GetRelatedTimers(RoundStatus roundStatus, TimeSpan? threshold = null)
                {
                    TimeSpan timeLimit = threshold?? TimeSpan.Zero;

                    return BridgeTimers.Where(t =>  t.Visibility    == Visibility.Visible
                                                &&  t.RemainingTime >= timeLimit
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
                        var limit = _startDate.AddMinutes(-30);
                        var now   = DateTime.Now;

                        if (now >= _startDate
                        &&  now <= limit)
                            return now;

                        return _startDate;
                    }

                    set
                    {
                        if (Set(ref _startDate, value))
                            setEndTime();
                    }
                }

                [JsonIgnore]
                public TimeOnly EndTime     { get; set; }

                [JsonIgnore]
                public bool     CanAndTimer { get; set; }

                [JsonIgnore]
                public int VisibleTimerCount
                {
                    get => _visibleTimerCount;
                    set
                    {
                        _visibleTimerCount = value;
                        CanAndTimer        = VisibleTimerCount <  4;
                    }
                }

                [JsonIgnore]
                public ObservableCollection<CustomColor> BackgroundColors => Global.GetBackgroundColors();

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
            public void Update(Configuration newConfiguration, bool updateUI = false)
            {
                ReadBridgeMate    = newConfiguration.ReadBridgeMate;
                ReadBC3           = newConfiguration.ReadBC3;
                BC3Path           = newConfiguration.BC3Path;
                ProjectorInterval = newConfiguration.ProjectorInterval;
                ProjectorMaxRows  = newConfiguration.ProjectorMaxRows;
                StartTime         = newConfiguration.StartTime;
                CultureName       = newConfiguration.CultureName;
                WindowOrientation = newConfiguration.WindowOrientation;

                Culture = LanguageService.Instance.SetCulture(CultureName);

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

                if (updateUI)
                    UpdateTimers();
            }

            #region Save / Load Configuration
                public void Save()
                {
                    ConfigVersion = _configVersion;

                    foreach (var preset in Presets.Where(p => p.CustomPreset == false))
                        preset.IsHidden = Presets.Any(p => p.CustomPreset == true && p.Name == preset.Name);

                    // Only Custom Presets are saved due to PresetCollectionConverter
                    string json = JsonSerializer.Serialize(this, _serializerOptions);
                    File.WriteAllText(_path, json);

                    setEndTime();
                }

                public void LoadLanguageSetting()
                {
                    Logger.Info("Loading language setting from Configuration file");

                    if (!File.Exists(_path)
                    &&  File.Exists(_oldPath))
                    {
                        // make sure that destination folder exists
                        Directory.CreateDirectory(Path.GetDirectoryName(_path));

                        // move old config file
                        File.Move(_oldPath, _path);

                        // Delete old folder if empty
                        string sourceFolder = Path.GetDirectoryName(_oldPath)!;

                        if (Directory.GetFiles(sourceFolder).Length       == 0
                        &&  Directory.GetDirectories(sourceFolder).Length == 0)
                            Directory.Delete(sourceFolder);
                    }

                    if (!File.Exists(_path)
                    ||  Arguments.Values.Lookup("mode") == "reset")
                        LanguageService.Instance.SetCulture("en-US");
                    else
                    {
                        var jsonData = File.ReadAllText(_path);

                        if (string.IsNullOrWhiteSpace(jsonData))
                            LanguageService.Instance.SetCulture("en-US");
                        else
                        {
                            Logger.Info("Reading Configuration file");

                            //TODO: Can later be removed 
                            if (jsonData.IndexOf("\"Color\":") >  -1)
                                jsonData = jsonData.Replace("\"Color\":", "\"BackgroundColor\":");

                            _loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonData, _serializerOptions);

                            LanguageService.Instance.SetCulture(CultureName);
                        }
                    }

                    Presets.AddRange([ new Preset(nameof(Lex.Pairs_7x4),  false, false, 7,  4,  4, 0, 27, 0, 1, 12, 5)
                                     , new Preset(nameof(Lex.Pairs_8x4),  false, false, 8,  4,  4, 0, 27, 0, 1, 10, 5)
                                     , new Preset(nameof(Lex.Pairs_9x3),  false, false, 9,  3,  5, 0, 21, 0, 1, 12, 5)
                                     , new Preset(nameof(Lex.Pairs_11x2), false, false, 11, 2,  6, 0, 14, 0, 1, 12, 5)
                                     , new Preset(nameof(Lex.Pairs_11x3), false, false, 11, 3,  6, 0, 21, 0, 1, 12, 5)
                                     , new Preset(nameof(Lex.Teams_2x16), false, true,  2,  16, 1, 1, 46, 0, 0, 15, 5)
                                     ]);

                    Logger.Info("Loaded  language setting from Configuration file");
                }

                public async Task LoadAsync()
                {
                    if (!File.Exists(_path)
                    ||  Arguments.Values.Lookup("mode") == "reset")
                    {
                        AppVersion    = _currentversion;
                        ConfigVersion = _configVersion;
                        Save();
                        await OpenSettingsAsync();
                    }
                    else
                    {
                        var jsonData = File.ReadAllText(_path);

                        if (string.IsNullOrWhiteSpace(jsonData))
                        {
                            AppVersion = _currentversion;
                            await OpenSettingsAsync();
                            Save();
                        }
                        else
                        {
                            if (_loadedConfig is null)
                            {
                                Logger.Info("Reading Configuration file");

                                //TODO: Can be removed when old config files are no longer in circulation
                                if (jsonData.IndexOf("\"Color\":") >  -1)
                                    jsonData = jsonData.Replace("\"Color\":", "\"BackgroundColor\":");

                                _loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonData, _serializerOptions);
                            }

                            Update(_loadedConfig);

                            if (_loadedConfig.ConfigVersion                           is null
                            ||  _loadedConfig.ConfigVersion.CompareTo(_configVersion) <  0)
                                await OpenSettingsAsync();
                        }
                    }

                    loadTimers();
                    IsLoaded = true;
                }
            #endregion

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

                    string json = JsonSerializer.Serialize(state, _serializerOptions);
                    File.WriteAllText(StatePath, json);
                }

                public void RestoreState()
                {
                {
                    if (File.Exists(StatePath))
                        try
                        {
                            Logger.Info("Restoring Timers");

                            var jsonData = File.ReadAllText(StatePath);
                            var state    = JsonSerializer.Deserialize<State>(jsonData, _serializerOptions);

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
                            Logger.Exception(ex, "Unable to restore state");
                            Debugger.Break();
                        }

                        finally { /* Ignore */ }
                }
                }
            #endregion

            public async Task OpenSettingsAsync()
            {
                var viewModel = IoC.Get<ConfigurationViewModel>();
                await _windowManager.ShowDialogAsync(viewModel);
            }

            public void OpenJSONFiles()
            {
                SaveState();
                tryOpenFile(StatePath);
                tryOpenFile(_path);
            }

            public void OpenLogFile()
            {
                tryOpenFile(Logger.LogFilePath);
            }
        #endregion

        #region Internal Timer Management Methods   
            internal void AddTimer()
            {
                if (VisibleTimerCount <  4)
                {
                    var bridgeTimer = BridgeTimers[^1];

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

            internal void UpdateTimers()
            {
                foreach (var bridgeTimer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                    bridgeTimer.UpdateDisplay();
            }

            #region internal Handle Timer acctions for all timers
                internal void StartAll()
                {
                    foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                        timer.Start();
                }

                internal void StopAll()
                {
                    foreach (var timer in BridgeTimers.Where(t => t.Visibility == Visibility.Visible))
                        timer.Stop();
                }

                internal void PauseAll()
                {
                    var list = BridgeTimers.Where(t => t.Visibility == Visibility.Visible).ToList();

                    if (list.Any(t => t.IsRunning))
                        StopAll();
                    else
                        StartAll();
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
            #endregion
        #endregion

        #region Private Methods
            private void loadTimers()
            {
                int i;
                BridgeTimers.Clear();

                for (i = 0; i <  4; i++)
                {
                    BridgeTimer timer;

                    if (_loadedConfig?.BridgeTimers      is null
                    ||  _loadedConfig.BridgeTimers.Count == 0)
                    {
                        timer = new BridgeTimer();
                        timer.Update(Presets[i]);
                        timer.Key             = null;
                        timer.BackgroundColor = BackgroundColors[i].Color;
                        timer.Groups          = (GroupFlags)(1 << i); // Set group to A, B, C or D

                        if (_visibleTimerCount >  1) // make sure that the first one is always visible
                            timer.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                        if (_loadedConfig.BridgeTimers.Count >  i)
                            timer = _loadedConfig.BridgeTimers[i];
                        else
                        {
                            timer = new();
                            timer.Update(Presets[i]);
                            timer.Visibility = System.Windows.Visibility.Collapsed;
                        }

                    if (timer.BreakAfterRound == 0
                    ||  timer.BreakMinutes    == 0)
                        timer.BreakAfterRound = timer.BreakMinutes = 0;

                    timer.SelectedSound = AudioResources.SoundDefinitions
                                                        .FirstOrDefault(s => s.DisplayName == timer.SelectedSound.DisplayName)
                                       ?? AudioResources.SoundDefinitions[i];

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
                    timer.Key             = null;
                    timer.BackgroundColor = BackgroundColors[i].Color;
                    timer.Groups          = (GroupFlags)(1 << i); // Set group to A, B, C or D
                    timer.Visibility      = Visibility.Collapsed;
                    timer.SelectedSound   = AudioResources.SoundDefinitions[i];

                    BridgeTimers.Add(timer);

                    if (timer.Visibility == Visibility.Visible)
                        VisibleTimerCount++;
                }

                if (_visibleTimerCount == 0)
                {
                    BridgeTimers[0].Visibility = Visibility.Visible;
                    BridgeTimers[0].UpdateDisplay();
                    _visibleTimerCount = 1;
                    Save();
                }

                arrangeTimers();
                SetUpDownVisibility();
                RestoreState();
            }

            private void arrangeTimers()
            {
                // Move collapsed timers at the end and enable/disable Close buttons
                //if (WindowOrientation == Orientation.Vertical
                //&& !string.IsNullOrEmpty(WindowOrientationIcon))
                //{   
                //    var order = new[] { 0, 2, 1, 3 };

                //    for (var i = BridgeTimers.Count - 1; i >= 0; i--)
                //    {
                //        var timer = BridgeTimers[i];

                //        if (timer.Visibility != Visibility.Visible)
                //            BridgeTimers.Move(i, BridgeTimers.Count - 1);
                //        else
                //            timer.CanClose = _visibleTimerCount > 1;
                //    }
                //}
                //else
                for (var i = BridgeTimers.Count - 1; i >= 0; i--)
                {
                    var timer = BridgeTimers[i];

                    if (timer.Visibility != Visibility.Visible)
                        BridgeTimers.Move(i, BridgeTimers.Count - 1);
                    else
                        timer.CanClose = _visibleTimerCount >  1;
                }
            }

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

        #region Private BridgeTimer Collection Change Handling
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

        #region Handle Change events on Presets Collection
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

