using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.UserControls;
using Syncfusion.Data.Extensions;
using Syncfusion.Windows.Tools.Controls;
using static System.TimeZoneInfo;

namespace DBF.DataModel
{
    public class Configuration : PropertyChangedBase
    {
        private static readonly string[] audioExtensions = new[] { ".wav", ".mp3" };
        private int VisibleTimerCount
        {
            get => visibleTimerCount;
            set
            {
                visibleTimerCount = value;
                TimersCanClose    = VisibleTimerCount >  1;
                TimersCanBeAdded  = VisibleTimerCount <  4;
            }
        }

        private string configDir, presetsPath, timerSettingPath, jsonData, jsonPresets, jsonTimers;
        //private List <TimerSetting> TimerSettings = new();
        #region Constructors
            public Configuration()
            {
                BackgroundColors = 
                [
                                 new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#F2460D"), ColorName = "Rød (dbf)" },
                                 new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#00b0ff"), ColorName = "Blå (dbf)" },
                                 new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#FF9D00"), ColorName = "Orange (dbf)" },
                                 new CustomColor() { Color = (Color)ColorConverter.ConvertFromString("#81C784"), ColorName = "Grøn (dbf)" },
                                 ];

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); // Roaming folder
                configDir          = Path.Combine(appDataPath, "DBFTools");
                presetsPath        = Path.Combine(configDir  , "presets.json");
                timerSettingPath   = Path.Combine(configDir  , "timerSetting.json");

                // Make sure that directory and files exists
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                // Store builtIn defaults
                if (!File.Exists(presetsPath))
                {
                    string json = JsonSerializer.Serialize( Presets.Where(p => p.CustomPreset == true).ToObservableCollection<Preset>()
                                                          , new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(presetsPath, json);
                }

                if (!File.Exists(timerSettingPath))
                {
                    string json = JsonSerializer.Serialize(BridgeTimers, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(timerSettingPath, json);
                }

                Load();
            }
        #endregion

        #region Public Properties
            public  ObservableCollection <CustomColor> BackgroundColors;
            private int                                visibleTimerCount = 0;

            public ObservableCollection<BridgeTimer> BridgeTimers     { get; set; } = new();

            public BindableCollection<Preset>        Presets          { get; set; } = new()
                                                                    {
                                                                            new Preset("Par - 7 runder af 4 spil", false, false,  7, 4, 4, 0,27, 0, 1, 12,5),
                                                                            new Preset("Par - 9 runder af 3 spil", false, false,  9, 3,  5,0, 20, 0, 1, 12,5),
                                                                            new Preset("Par - 11 runder af 2 spil",false, false, 11, 2,  6,0,13, 0, 1, 12,5),
                                                                            new Preset("Hold kamp af 32 spil",     false, true,   2, 16, 1,1,28, 0, 0,15,5)
                                                                    };

            public bool                              TimersCanClose   { get; set; }
            public bool                              TimersCanBeAdded { get; set; }
        #endregion

        #region Private and Internal methods
            #region Load and Save methods
                internal void Load()
                {
                    Assembly assembly = Assembly.GetExecutingAssembly();
                    var      names    = assembly.GetManifestResourceNames();

                    foreach (var name in names.Where(n => n.StartsWith("DBF.AudioFiles.")))
                        Console.WriteLine(name);

                    jsonData          = File.ReadAllText(presetsPath);
                    var loadedPresets = JsonSerializer.Deserialize<ObservableCollection<Preset>>(jsonData);

                    foreach (var preset in loadedPresets)
                        if (Presets.FirstOrDefault(p => p.Name == preset.Name) == null)
                            Presets.Add(preset);

                    jsonData         = File.ReadAllText(timerSettingPath);
                    var loadedTimers = JsonSerializer.Deserialize<ObservableCollection<BridgeTimer>>(jsonData);
                    int i;

                    for (i = 0; i <  4; i++)
                    {
                        BridgeTimer timer;

                        if (loadedTimers is null || loadedTimers.Count == 0)
                        {
                            timer = new BridgeTimer();
                            timer.Update(Presets[i]);
                            timer.Name  = null;
                            timer.Color = BackgroundColors[i].Color;
                            timer.Group = ((char)('A' + i)).ToString(); // Set group to A, B, C or D
                        }
                        else
                            if (loadedTimers.Count >  i)
                                timer = loadedTimers[i];
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

                    for (i = BridgeTimers.Count - 1; i >= 0; i--)
                    {
                        var timer = BridgeTimers[i];

                        if (timer.Visibility != Visibility.Visible)
                        {
                            BridgeTimers.Remove(timer);
                            BridgeTimers.Add(timer);
                        }
                    }
                }

                internal void SavePresets()
                {
                    jsonPresets = JsonSerializer.Serialize<ObservableCollection<Preset>>(Presets.Where(p => p.CustomPreset == true).ToObservableCollection<Preset>());

                    File.WriteAllText(presetsPath, jsonPresets);
                }

                internal void SaveSettings()
                {
                    jsonTimers = JsonSerializer.Serialize<ObservableCollection<BridgeTimer>>(BridgeTimers);

                    File.WriteAllText(timerSettingPath, jsonTimers);

                    VisibleTimerCount = BridgeTimers.Count(ts => ts.Visibility == Visibility.Visible);
                }

                internal void Save()
                {
                    SavePresets();
                    SaveSettings();
                }
            #endregion

            #region Private and Internal Methods              
                internal void AddTimer()
                {
                    foreach (var bridgeTimer in BridgeTimers)
                        if (bridgeTimer.Visibility != Visibility.Visible)
                        {
                            bridgeTimer.Visibility = Visibility.Visible;
                            VisibleTimerCount++;
                            SaveSettings();
                            return;
                        }
                }

                internal void CloseTimer(BridgeTimer timer)
                {
                    var result = MessageBox.Show("Dette nulstiller uret fuldtsændigt. Vil du nulstille uret?", "Bekræftelse", MessageBoxButton.OKCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.OK)
                    {
                        VisibleTimerCount--;
                        timer.Reset();
                        timer.Visibility = Visibility.Collapsed;
                        BridgeTimers.Remove(timer);
                        BridgeTimers.Add(timer);
                        SaveSettings();
                    }
                }
            #endregion
        #endregion
    }
}

