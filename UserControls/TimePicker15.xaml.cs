using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace DBF.UserControls
{
    public partial class TimePicker15 : UserControl
    {
        #region Constructors
            public TimePicker15()
            {
                InitializeComponent();
                GenerateTimes();
            }
        #endregion

        #region Public Properties
            public ObservableCollection<TimeOnly> Times { get; } = new();
        #endregion

        #region Dependency Properties
            #region Dependency Properties - SelectedTime 
                public static readonly DependencyProperty SelectedTimeProperty = 
                                       DependencyProperty.Register( nameof(SelectedTime), typeof(TimeOnly?), typeof(TimePicker15)
                                                                  , new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged));

                public TimeOnly? SelectedTime
                {
                    get => (TimeOnly?)GetValue(SelectedTimeProperty);
                    set => SetValue(SelectedTimeProperty, value);
                }
            #endregion

            #region Dependency Properties - IntervalMinutes
                public static readonly DependencyProperty IntervalMinutesProperty = 
                                       DependencyProperty.Register( nameof(IntervalMinutes), typeof(int), typeof(TimePicker15)
                                                                  , new PropertyMetadata(15, OnIntervalChanged));

                public int IntervalMinutes
                {
                    get => (int)GetValue(IntervalMinutesProperty);
                    set => SetValue(IntervalMinutesProperty, value);
                }
            #endregion
        #endregion

        #region Private methods
            private static void OnIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is TimePicker15 tp)
                    tp.GenerateTimes();
            }

            private void GenerateTimes()
            {
                Times.Clear();
                int interval = Math.Max(1, IntervalMinutes);

                for (int h = 0; h <  24; h++)
                    for (int m = 0; m <  60; m += interval)
                        Times.Add(new TimeOnly(h, m));

                if (SelectedTime.HasValue)
                {
                    var snapped  = SnapToInterval(SelectedTime.Value);
                    SelectedTime = snapped;
                }
            }

            private TimeOnly SnapToInterval(TimeOnly t)
            {
                int totalMinutes   = t.Hour * 60 + t.Minute;
                int interval       = Math.Max(1, IntervalMinutes);
                int snappedMinutes = (totalMinutes / interval) * interval;
                int hh             = snappedMinutes / 60;
                int mm             = snappedMinutes % 60;
                return new TimeOnly(hh, mm);
            }

            private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is TimePicker15 tp)
                    if (e.NewValue is TimeOnly newTime)
                    {
                        var snapped = tp.SnapToInterval(newTime);

                        if (!snapped.Equals(newTime))
                            tp.SelectedTime = snapped;

                        tp.PART_Combo.SelectedItem = snapped;
                    }
                    else
                        tp.PART_Combo.SelectedItem = null;
            }
        #endregion
    }
}
