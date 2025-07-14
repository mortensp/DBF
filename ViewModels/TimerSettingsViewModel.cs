using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.DataModel;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.ViewModels
{
    public class TimerSettingViewModel : Screen
    {
        private       Preset                            selectedPreset     { get; set; }
        private          TimerSetting   setting;
        private readonly IWindowManager _windowManager;

        #region Constructors
            public TimerSettingViewModel(Configuration configuration)
            {
                Configuration               = configuration;
                _windowManager              = IoC.Get<IWindowManager>();
                //NewSetting.PropertyChanged += onSettingPropertyChanged;
            }
        #endregion

        #region public Properties
            public static ObservableCollection<CustomColor> NewColorCollection { get; set; } = new()
                                                {
                                                    new CustomColor { ColorName = "Rød", Color = (Color)ColorConverter.ConvertFromString("#F2460D")},
                                                    new CustomColor { ColorName = "Blå", Color = (Color)ColorConverter.ConvertFromString("#00b0ff") },
                                                    new CustomColor { ColorName = "range", Color = (Color)ColorConverter.ConvertFromString("#FF9D00")},
                                                    new CustomColor { ColorName = "Grøn", Color = (Color)ColorConverter.ConvertFromString("#81C784")},
                                                };

            public        Configuration                     Configuration      { get; private set; }
            public        TimerSetting                      NewSetting         { get; set; } = new();

            public        Color                             BackgroundColor    { get; set; }
            public Preset SelectedPreset
            {
                get => selectedPreset;
                set
                {
                    selectedPreset = value;

                    if (value != null)
                        NewSetting.Update(value);
                }
            }

            public bool CustomPreset => selectedPreset is not null && selectedPreset.CustomPreset == true;

            public TimerSetting Setting
            {
                get => setting;
                set
                {
                    Set(ref setting, value);
                    NewSetting.Update(value);

                    selectedPreset = FindPreset(NewSetting);
                    NotifyOfPropertyChange(nameof(SelectedPreset));
                    //    NotifyOfPropertyChange(nameof(Setting));
                }
            }
        #endregion

        #region Public Methods
            public async void Cancel()
            {
                await TryCloseAsync();
            }

            public async void AcceptSetting()
            {
                if (selectedPreset is not null && !selectedPreset.Matches(NewSetting))
                    if (selectedPreset.CustomPreset)
                    {
                        var result = MessageBox.Show("Vil du gemme dine ændringer i din forudstilling?", "Bekræftelse", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (result == MessageBoxResult.Cancel)
                            return;

                        if (result == MessageBoxResult.Yes)
                            SavePreset();
                    }

                Setting.Update(NewSetting);
                await TryCloseAsync();
                Configuration.SaveSettings();
            }

            public async void AddPreset()
            {
                var dialog = IoC.Get<PresetNameViewModel>();
                await _windowManager.ShowDialogAsync(dialog);

                if (!string.IsNullOrEmpty(dialog.PresetName))
                {
                    NewSetting.Name = dialog.PresetName;
                    var preset      = new Preset(NewSetting);
                    Configuration.Presets.Add(preset);

                    Configuration.SavePresets();
                    SelectedPreset = preset;
                }
            }

            public void SavePreset()
            {
                SelectedPreset.Update(NewSetting);
                Configuration.SavePresets();
            }

            public void DeletePreset()
            {
                if (SelectedPreset.CustomPreset)
                {
                    Configuration.Presets.Remove(selectedPreset);
                    Configuration.SavePresets();
                    NewSetting.Name = null;
                    SelectedPreset  = FindPreset(NewSetting);
                }
            }

            public void VolumeChanged(RoutedPropertyChangedEventArgs<double> e)
            {
                double newValue = e.NewValue;
                AudioPlayer.Play(NewSetting.Sound, (int)newValue);
            }

            public void SoundChanged()
            {
                AudioPlayer.Play(NewSetting.Sound, (int)NewSetting.Volume);
            }

            private Preset FindPreset(Preset preset) => Configuration.Presets.FirstOrDefault(p => p.Matches(preset));
        #endregion

        #region Private Methods
            private void onSettingPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                if (sender is Preset preset)
                {
                    if (selectedPreset is not null
                    && preset.Matches(selectedPreset))
                       return;
                    
                    preset.Name = null;
                    var newPreset = FindPreset(preset);

                if (newPreset is not null )
                if (selectedPreset is null)
                    SelectedPreset = newPreset;
                else
                    SelectedPreset.Name = newPreset?.Name;
                }
            }
        #endregion
    }
}