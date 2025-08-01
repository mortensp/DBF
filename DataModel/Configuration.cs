using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.UserControls;
using Microsoft.Extensions.Options;
using Syncfusion.Data.Extensions;
using Syncfusion.Windows.Tools.Controls;
using static System.TimeZoneInfo;

namespace DBF.DataModel
{
    public partial class Configuration : PropertyChangedBase
    {
        private static readonly string[]             audioExtensions   = new[] { ".wav", ".mp3" };
        public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions { WriteIndented = true, };
        private static         string                currentversion    = "v" + Assembly.GetExecutingAssembly().GetName().Version;
        private static         string                path              = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DBFTools\\configuration.json";
        private                Configuration         loadedConfig      = null;
        private                int                   visibleTimerCount = 0;

        #region Constructors
            static Configuration()
            {
                SerializerOptions.Converters.Add(new PresetCollectionConverter());

                // Make sure that Roaming folder exists
                var configDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DBFTools";

                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                //Assembly assembly = Assembly.GetExecutingAssembly();
                //var names = assembly.GetManifestResourceNames();

                //foreach (var name in names.Where(n => n.StartsWith("DBF.AudioFiles.")))
                //    Console.WriteLine(name);
            }

            public Configuration()
            {
            }

            //public configuration(configuration loaded)
            //{
            //    Update(loaded);
            //}
        #endregion

        #region Public Properties
            #region Public Properties - Serilizable
                public string                            AppVersion        { get; private set; } = currentversion;
                public int                               ProjectorInterval { get; set; } = 20;
                public int                               ProjectorMaxRows  { get; set; } = 40;

                public ObservableCollection<BridgeTimer> BridgeTimers      { get; set; } = new();
            #endregion

            #region Public Properties - JsonIgore
                [JsonIgnore]
                public bool                              TimersCanBeAdded  { get; set; }

                [JsonIgnore]
                public int VisibleTimerCount
                {
                    get => visibleTimerCount;
                    set
                    {
                        visibleTimerCount = value;
                        //TimersCanClose   = VisibleTimerCount > 1;
                        TimersCanBeAdded = VisibleTimerCount <  4;
                    }
                }

                [JsonIgnore]
                public BindableCollection<Preset>        Presets           { get; set; } = new()
                                                        {
                                                           new Preset("Par - 7 runder af 4 spil", false, false,  7, 4, 4, 0,27, 0, 1, 12,5),
                                                           new Preset("Par - 9 runder af 3 spil", false, false,  9, 3,  5,0, 20, 0, 1, 12,5),
                                                           new Preset("Par - 11 runder af 2 spil",false, false, 11, 2,  6,0,13, 0, 1, 12,5),
                                                           new Preset("Hold kamp af 32 spil",     false, true,   2, 16, 1,1,26, 0, 0,15,5)
                                                        };

                [JsonIgnore]
                public ObservableCollection <CustomColor> BackgroundColors = new()
                                                        {
                                                            new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#F2460D"), ColorName = "Rød (dbf)" },
                                                            new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#00b0ff"), ColorName = "Blå (dbf)" },
                                                            new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#FF9D00"), ColorName = "Orange (dbf)" },
                                                            new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#81C784"), ColorName = "Grøn (dbf)" },
                                                        };

                [JsonIgnore]
                public bool TimersActive
                {
                    get
                    {
                        for (int i = 0; i <  VisibleTimerCount; i++)
                            if (BridgeTimers[i].IsStarted)
                                return true;

                        return false;
                    }
                }
            #endregion
        #endregion

        #region Public Methods
            public void Load()
            {
                if (!File.Exists(path))
                {
                    AppVersion = currentversion;
                    Save();
                }
                else
                {
                    var jsonData = File.ReadAllText(path);
                    loadedConfig = JsonSerializer.Deserialize<Configuration>(jsonData, SerializerOptions);

                    Update(loadedConfig);
                }

                loadTimers();
            }

            private void loadTimers()
            {
                int i;
                BridgeTimers = new();

                for (i = 0; i <  4; i++)
                {
                    BridgeTimer timer;

                    if (loadedConfig?.BridgeTimers is null || loadedConfig.BridgeTimers.Count == 0)
                    {
                        timer = new BridgeTimer();
                        timer.Update(Presets[i]);
                        timer.Name  = null;
                        timer.Color = BackgroundColors[i].Color;
                        timer.Group = ((char)('A' + i)).ToString(); // Set group to A, B, C or D

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

                    if (string.IsNullOrEmpty(timer.Sound))
                        timer.Sound = AudioPlayer.Sounds[i];

                    timer.UpdateDisplay();
                    BridgeTimers.Add(timer);

                    if (timer.Visibility == Visibility.Visible)
                        VisibleTimerCount++;
                }

                for (; i <  4; i++)
                {
                    var timer = new BridgeTimer();
                    timer.Update(Presets[i]);
                    timer.Name       = null;
                    timer.Color      = BackgroundColors[i].Color;
                    timer.Group      = ((char)('A' + i)).ToString(); // Set group to A, B, C or D
                    timer.Visibility = Visibility.Collapsed;
                    timer.Sound      = AudioPlayer.Sounds[i];

                    BridgeTimers.Add(timer);

                    if (timer.Visibility == Visibility.Visible)
                        VisibleTimerCount++;
                }

                // Put Collapsed timers at the end
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
                    visibleTimerCount          = 1;
                }

                SetUpDownVisibility();
            }

            public void Save()
            {
                string json = JsonSerializer.Serialize(this, SerializerOptions);
                File.WriteAllText(path, json);
            }

            public void Update(Configuration newConfiguration)
            {
                if (newConfiguration.hasUserValues)
                {
                    ProjectorInterval = newConfiguration.ProjectorInterval;
                    ProjectorMaxRows  = newConfiguration.ProjectorMaxRows;
                }

                // Keep built-in Presets
                foreach (var preset in newConfiguration.Presets)
                    if (Presets.FirstOrDefault(p => p.Name == preset.Name) == null)
                        Presets.Add(preset);
            }
        #endregion

        #region Private and Internal methods
            #region Private and Internal Methods          
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

                internal void AddTimer()
                {
                    if (VisibleTimerCount <  4)
                    {
                        var bridgeTimer = BridgeTimers[VisibleTimerCount];

                        bridgeTimer.Visibility = Visibility.Visible;

                        VisibleTimerCount++;
                        Save();
                        SetUpDownVisibility();
                    }
                }

                internal void CloseTimer(BridgeTimer timer)
                {
                    if (!timer.IsStarted
                    ||  MessageBoxResult.OK == MessageBox.Show( "Dette nulstiller uret fuldtsændigt. Vil du nulstille uret?"
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
    }
}

