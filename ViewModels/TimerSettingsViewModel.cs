using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.DataModel;
using DBF.Helpers;
using Localization;
using Syncfusion.Windows.Tools.Controls;

namespace DBF.ViewModels;

public class TimerSettingsViewModel : Screen
{
    private IAudioService _player;
    private Preset selectedPreset { get; set; }
    private          BridgeTimer    setting;
    private readonly IWindowManager windowManager;
    private          bool           onOpen;

    #region Constructors
        public TimerSettingsViewModel()
        {
            if (!Design.IsInDesignMode())
                throw new InvalidOperationException("Use TimerSettingsViewModel(Configuration configuration) constructor.");

            Configuration = new();
            _             = Configuration.LoadAsync();
            Setting       = Configuration.BridgeTimers.First();

            NewColorCollection         = new ObservableCollection<CustomColor>(Configuration.BackgroundColors);
            NewSetting.PropertyChanged+= newSetting_PropertyChanged;
        }

        public TimerSettingsViewModel(Configuration configuration)
        {
            Configuration              = configuration;
            NewColorCollection         = new ObservableCollection<CustomColor>(Configuration.BackgroundColors);
            NewSetting.PropertyChanged+= newSetting_PropertyChanged;

            if (Design.IsInDesignMode())
            {
                _       = Configuration.LoadAsync();
                Setting = Configuration.BridgeTimers.First();
            }
            else
            {
                _player       = IoC.Get<IAudioService>();
                windowManager = IoC.Get<IWindowManager>();
            }
        }
    #endregion

    #region public Properties
        public string Message
        {
            get => field ?? (NewSetting.ChangedProperties.Count >  0 ? Lex.MarkedValues : null);
            set => field = value;
        }

        public static ObservableCollection<CustomColor> NewColorCollection { get; private set; }
        public        Configuration                     Configuration      { get; private set; }
        public        TimerSetting                      NewSetting         { get; set; } = new();
        public        Color                             BackgroundColor    { get; set; }
        public        Color                             ForegroundColor    { get; set; }

        public        TimeOnly?                         EndTime            => Configuration.StartTime?.AddMinutes(NewSetting.Duration);

        public Preset SelectedPreset
        {
            get => selectedPreset;
            set
            {
                selectedPreset = value;

                if (value != null && !onOpen)
                    NewSetting.Update(value);
            }
        }

        public bool CustomPreset => selectedPreset is not null && selectedPreset.CustomPreset == true;

        public BridgeTimer Setting
        {
            get => setting;
            set
            {
                if (Set(ref setting, value))
                {
                    onOpen = true;
                    NewSetting.Update(value);
                    SelectedPreset = FindPreset(NewSetting);
                    onOpen         = false;
                }
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

            Setting.Update(NewSetting);

            if (selectedPreset is not null
            && !selectedPreset.Matches(NewSetting))
                if (selectedPreset.CustomPreset)
                {
                    var result = MessageBox.Show($"{Lex.SaveSettingsQuestion}?", Lex.Confirmation, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Cancel)
                        return;

                    if (result == MessageBoxResult.Yes)
                        SavePreset();
                }

            await TryCloseAsync();
            Configuration.Save();
            Logger.Info("Timer setting changed");

        }

        public async void AddPreset()
        {
            var dialog = IoC.Get<PresetNameViewModel>();
            await windowManager.ShowDialogAsync(dialog);

            if (!string.IsNullOrEmpty(dialog.PresetName))
            {
                NewSetting.Name = dialog.PresetName;
                var preset      = new Preset(NewSetting);
                Configuration.Presets.Add(preset);
                Configuration.Save();
                SelectedPreset = preset;
                Logger.Info("New Preset added");
            }
        }

        public void SavePreset()
        {
            if (!NewSetting.CustomPreset)
            {
                var existing =FindPreset(NewSetting,true);

                if (existing is not null)
                {
                    SelectedPreset = existing;
                    return;
                }

                var preset  = new Preset(NewSetting);
                var name    = SelectedPreset.Name;
                var buildin = Configuration.Presets.FirstOrDefault(p => p.Name == name);

                if (buildin != null)
                    buildin.IsHidden = true;

                preset.Name     = name;
                preset.IsHidden = false;

                Configuration.Presets.Add(preset);

                Configuration.Save();
                SelectedPreset = preset;
                Logger.Info("Buildin Preset '{NewSetting.Name}' changed");
            }
            else
            {
                NewSetting.CustomPreset = true;
                SelectedPreset.Update(NewSetting);
                Configuration.Save();
                Logger.Info($"Preset '{NewSetting.Name}' changed");
            }
        }

        public void DeletePreset()
        {
            if (SelectedPreset.CustomPreset)
            {
                var name    = SelectedPreset.Name;
                var buildin = Configuration.Presets.FirstOrDefault(p => p.Name == name);

                if (buildin != null)
                    buildin.IsHidden = false;

                Configuration.Presets.Remove(selectedPreset);
                Configuration.Save();
                NewSetting.Name = null;
                SelectedPreset  = FindPreset(NewSetting);
                Logger.Info($"Preset '{NewSetting.Name}' deleted");

                if (buildin != null)
                    Logger.Info($"Buildin Preset '{NewSetting.Name}' restored");
            }
        }

        public void VolumeChanged(RoutedPropertyChangedEventArgs<double> e)
        {
            double newValue = e.NewValue;

            if (!onOpen)
                _player.Play(NewSetting.Sound, (int)newValue);
        }

        public void SoundChanged()
        {
            _player.Play(NewSetting.Sound, (int)NewSetting.Volume);
        }
    #endregion

    #region Private Methods
        private Preset FindPreset(Preset preset, bool withoutName = false) => Configuration.Presets.FirstOrDefault(p => p.Matches(preset, withoutName));

        private void newSetting_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // When the duration or the start time of the new setting changes,
            // notify that EndTime has changed. Also notify StartTime so bindings update.
            if (e.PropertyName == nameof(NewSetting.Duration))
            {
                //NotifyOfPropertyChange(nameof(StartTime));
                NotifyOfPropertyChange(nameof(EndTime));
            }
        }

        private void onSettingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Preset preset)
            {
                if (selectedPreset is not null
                &&  preset.Matches(selectedPreset))
                    return;

                preset.Name   = null;
                var newPreset = FindPreset(preset);

                if (newPreset is not null)
                    if (selectedPreset is null)
                        SelectedPreset = newPreset;
                    else
                        SelectedPreset.Name = newPreset?.Name;
            }
        }
    #endregion

    protected override Task OnDeactivateAsync(bool close, CancellationToken cancellationToken)
    {
        if (close)
        {
            if (NewSetting is not null)
                NewSetting.PropertyChanged -= newSetting_PropertyChanged;
        }

        return base.OnDeactivateAsync(close, cancellationToken);
    }

    internal void SuggestedSettings(Preset suggested)
    {
        // Finally update the editable NewSetting so the UI shows the suggested values
        var name =selectedPreset?.Name;

        NewSetting.MarkChanged(suggested);

        if (NewSetting.ChangedProperties.Count == 0)
            return;

        SelectedPreset = FindPreset(suggested);

        if (SelectedPreset?.Name != name)
            NewSetting.ChangedProperties.Add(nameof(NewSetting.Name));

        NotifyOfPropertyChange(nameof(Message));
    }
}
