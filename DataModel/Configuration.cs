using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.AudioServices;
using Syncfusion.Data.Extensions;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.DataModel
{
    public partial class Configuration : PropertyChangedBase
    {
        private static readonly string[] audioExtensions = new[] { ".wav", ".mp3" };
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true, };
        private static string currentversion = "v" + Assembly.GetExecutingAssembly().GetName().Version;
        private static string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Mortensp\\DBF\\configuration.json";
        private static string oldPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DBFTools\\configuration.json";
        private Configuration loadedConfig;
        private int visibleTimerCount;
        private static readonly TimeSpan _fiveHours = new TimeSpan(5, 0, 0);
        private static readonly TimeSpan _zeroTime = new TimeSpan(0, 0, 0);
        private DateTime startTime;

        #region Constructors
        static Configuration()
        {
            SerializerOptions.Converters.Add(new PresetCollectionConverter());

            // Make sure that Roaming folder exists
            var configDir = Path.GetDirectoryName(path);

            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);
        }

        public Configuration()
        {
            BridgeTimers.CollectionChanged += BridgeTimers_CollectionChanged;
        }
        #endregion

        #region Public Properties
        #region Public Properties - Serilizable
        public string AppVersion { get; private set; } = currentversion;
        public string BC3Path
        {
            get => bc3Path;
            set
            {
                bc3Path = value.Trim();

                if (string.IsNullOrWhiteSpace(bc3Path))
                    bc3Path = @"C:\BC3\";
                else
                    if (!bc3Path.EndsWith('/'))
                    bc3Path += '/';
            }
        }

        public int ProjectorInterval { get; set; } = 20;
        public int ProjectorMaxRows { get; set; } = 40;

        public string HomepagePath => bc3Path + @"Hjemmeside\";
        public string BridgeMatePath => bc3Path + @"BridgeMate\";
        public ObservableCollection<BridgeTimer> BridgeTimers { get; set; } = new();
        #endregion

        #region Public Properties - JsonIgore
        [JsonIgnore]
        public DateTime StartTime
        {
            get
            {
                var active = BridgeTimers.Where(t => t.Visibility == Visibility.Visible && !t.IsEnded);
                double duration = active.Any() ? active.Max(t => t.MinutesLeft) : 0;

                var limit = TimeSpan.FromMinutes(30 + duration); // tillad 30 min. forsinkelse
                var diff = DateTime.Now - startTime;

                if (diff > _zeroTime && diff < limit)
                
                    return DateTime.Now;
                    
                
                else
                    return startTime;
            }

            set
            {
                if (Set(ref startTime, value))
                    setEndTime();
            }
        }

        [JsonIgnore] public TimeOnly EndTime { get; set; }

        [JsonIgnore] public bool TimersCanBeAdded { get; set; }

        [JsonIgnore]
        public int VisibleTimerCount
        {
            get => visibleTimerCount;
            set
            {
                visibleTimerCount = value;
                TimersCanBeAdded = VisibleTimerCount < 4;
            }
        }

        public BindableCollection<Preset> Presets { get; set; } = new()
                                                                                                                                                                                                                                                        {
                          new Preset("Par - 7 runder af 4 spil",  false, false, 7,  4,  4, 0, 27, 0, 1, 12, 5)
                        , new Preset("Par - 9 runder af 3 spil",  false, false, 9,  3,  5, 0, 21, 0, 1, 12, 5)
                        , new Preset("Par - 11 runder af 2 spil", false, false, 11, 2,  6, 0, 14, 0, 1, 12, 5)
                        , new Preset("Hold kamp af 32 spil",      false, true,  2,  16, 1, 1, 46, 0, 0, 15, 5)
                                                                                                                                                                                                                                                        };

        [JsonIgnore]
        public ObservableCollection<CustomColor> BackgroundColors = new()
                {
                     new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#FFFFFF"), ColorName = "Hvid" }
                    ,new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#F2460D"), ColorName = "Rød (dbf)" }
                    ,new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#FF66CCFF"), ColorName = "Blå (dbf)" }
                    ,new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#FF9D00"), ColorName = "Orange (dbf)" }
                    ,new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#81C784"), ColorName = "Grøn (dbf)" }
                };

        private string bc3Path = @"C:\BC3\";

        [JsonIgnore]
        public bool TimersActive
        {
            get
            {
                for (int i = 0; i < VisibleTimerCount; i++)
                    if (BridgeTimers[i].IsActive)
                        return true;

                return false;
            }
        }
        #endregion
        #endregion

        #region Public Methods
        public void Load()
        {
            if (!File.Exists(path)
            && File.Exists(oldPath))
            {
                // Sørg for at destination-mappen eksisterer
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                // Flyt gammel config fil
                File.Move(oldPath, path);

                // Slet evt. gammel tom mappe
                string sourceFolder = Path.GetDirectoryName(oldPath)!;
                if (Directory.GetFiles(sourceFolder).Length == 0 
                &&  Directory.GetDirectories(sourceFolder).Length == 0)
                    Directory.Delete(sourceFolder);
            }

            if (!File.Exists(path))
            {
                AppVersion = currentversion;
                Save();
            }
            else
            {
                var jsonData = File.ReadAllText(path);

                //TODO: kan fjernes senere
                if (jsonData.IndexOf("\"BackgroundColor\":") == -1)
                    jsonData = jsonData.Replace("\"Color\":", "\"BackgroundColor\":");

                loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonData, SerializerOptions);

                Update(loadedConfig);
            }

            loadTimers();
        }

        private void loadTimers()
        {
            int i;
            BridgeTimers.Clear();

            for (i = 0; i < 4; i++)
            {
                BridgeTimer timer;

                if (loadedConfig?.BridgeTimers is null || loadedConfig.BridgeTimers.Count == 0)
                {
                    timer = new BridgeTimer();
                    timer.Update(Presets[i]);
                    timer.Name = null;
                    timer.BackgroundColor = BackgroundColors[i].Color;
                    timer.Groups = (GroupFlags)(1 << i); // Set group to A, B, C or D

                    if (visibleTimerCount > 1) // Lad som standard de to første være visible
                        timer.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                    if (loadedConfig.BridgeTimers.Count > i)
                    timer = loadedConfig.BridgeTimers[i];
                else
                {
                    timer = new();
                    timer.Update(Presets[i]);
                    timer.Visibility = System.Windows.Visibility.Collapsed;
                }

                if (string.IsNullOrEmpty(timer.Sound))
                        timer.Sound = AudioResources.Sounds[i];

                BridgeTimers.Add(timer);

                if (timer.Visibility == Visibility.Visible)
                {
                    timer.UpdateDisplay();
                    VisibleTimerCount++;
                }
            }

            for (; i < 4; i++)
            {
                var timer = new BridgeTimer();
                timer.Update(Presets[i]);
                timer.Name = null;
                timer.BackgroundColor = BackgroundColors[i].Color;
                timer.Groups = (GroupFlags)(1 << i); // Set group to A, B, C or D
                timer.Visibility = Visibility.Collapsed;
               timer.Sound           = AudioResources.Sounds[i];


                BridgeTimers.Add(timer);

                if (timer.Visibility == Visibility.Visible)
                    VisibleTimerCount++;
            }

            // Move collapsed timers at the end
            for (i = BridgeTimers.Count - 1; i >= 0; i--)
            {
                var timer = BridgeTimers[i];

                if (timer.Visibility != Visibility.Visible)
                {
                    BridgeTimers.Remove(timer);
                    BridgeTimers.Add(timer);
                }
            }

            if (visibleTimerCount == 0)
            {
                BridgeTimers[0].Visibility = Visibility.Visible;
                visibleTimerCount = 1;
            }

            SetUpDownVisibility();
        }

        public void Save()
        {
            // Only Custom Presets are saved due to PresetCollectionConverter
            string json = JsonSerializer.Serialize(this, SerializerOptions);
            File.WriteAllText(path, json);

            setEndTime();
        }

        public void Update(Configuration newConfiguration)
        {
            BC3Path = newConfiguration.BC3Path;
            ProjectorInterval = newConfiguration.ProjectorInterval;
            ProjectorMaxRows = newConfiguration.ProjectorMaxRows;

            if (newConfiguration.hasUserValues)
            {
                ProjectorInterval = newConfiguration.ProjectorInterval;
                ProjectorMaxRows = newConfiguration.ProjectorMaxRows;
            }

            // Keep built-in Presets
            foreach (var preset in newConfiguration.Presets)
                if (Presets.FirstOrDefault(p => p.Name == preset.Name) == null)
                    Presets.Add(preset);
        }
        #endregion

        #region Internal Timer Management Methods   
        internal void AddTimer()
        {
            if (VisibleTimerCount < 4)
            {
                var bridgeTimer = BridgeTimers[VisibleTimerCount];

                bridgeTimer.Visibility = Visibility.Visible;

                bridgeTimer.UpdateDisplay();
                VisibleTimerCount++;
                Save();
                SetUpDownVisibility();
            }
        }

        internal void CloseTimer(BridgeTimer timer)
        {
            if (!timer.IsStarted
            || MessageBoxResult.OK == MessageBox.Show("Dette nulstiller uret fuldtsændigt. Vil du nulstille uret?"
                                                      , "Bekræftelse"
                                                      , MessageBoxButton.OKCancel
                                                      , MessageBoxImage.Question))
            {
                VisibleTimerCount--;
                timer.Reset(false);
                timer.Visibility = Visibility.Collapsed;

                // Move the collapsed timer to the end of the list
                BridgeTimers.Remove(timer);
                BridgeTimers.Add(timer);

                Save();
                SetUpDownVisibility();
            }
        }

        internal void TimerUp(BridgeTimer timer)
        {
            if (timer.Visibility == Visibility.Visible)
            {
                var i = BridgeTimers.IndexOf(timer);
                var gem = BridgeTimers[i - 1];
                BridgeTimers[i - 1] = timer;
                BridgeTimers[i] = gem;

                Save();
                SetUpDownVisibility();
            }
        }

        internal void TimerDown(BridgeTimer timer)
        {
            if (timer.Visibility == Visibility.Visible)
            {
                var i = BridgeTimers.IndexOf(timer);
                var gem = BridgeTimers[i + 1];
                BridgeTimers[i + 1] = timer;
                BridgeTimers[i] = gem;

                Save();
                SetUpDownVisibility();
            }
        }
        #endregion

        #region Private Methods
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
                EndTime = active.Max(t => t.EndTime);
        }

        private void SetUpDownVisibility()
        {
            foreach (var timer in BridgeTimers)
            {
                timer.ShowUpButton = Visibility.Visible;
                timer.ShowDownButton = Visibility.Visible;
            }

            BridgeTimers[0].ShowUpButton = Visibility.Collapsed;
            BridgeTimers[VisibleTimerCount - 1].ShowDownButton = Visibility.Collapsed;
        }
        #endregion

        #region private BridgeTimer Collection Change Handling
        private void BridgeTimers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
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
    }
}

