using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;

using DragDropEffects = System.Windows.DragDropEffects;
using DragEventArgs = System.Windows.DragEventArgs;

namespace DBF.UserControls;

public partial class TimersPanel : UserControl, INotifyPropertyChanged
{
    public Guid Id { get; } = Guid.NewGuid();

    #region Constructors
        public TimersPanel(Visibility buttonsVisibility)
        {
            InitializeComponent();

            ButtonsVisibility = buttonsVisibility;
        }

        public TimersPanel()
        {
            InitializeComponent();
        }
    #endregion

    #region Public Properties
        public BridgeTimer BridgeTimer0 { get; private set; }
        public BridgeTimer BridgeTimer1 { get; private set; }
        public BridgeTimer BridgeTimer2 { get; private set; }
        public BridgeTimer BridgeTimer3 { get; private set; }
        public Configuration Configuration
        {
            get         => field ?? IoC.Get<Configuration>();
            private set => field = value;
        }
    #endregion

    #region Dependency Properties
        #region Dependency Property BridgeTimers
            public BindableCollectionExt<BridgeTimer> BridgeTimers
            {
                get => (BindableCollectionExt<BridgeTimer>)GetValue(BridgeTimersProperty);
                set => SetValue(BridgeTimersProperty, value);
            }

            public static readonly DependencyProperty BridgeTimersProperty = 
                                   DependencyProperty.Register( nameof(BridgeTimers)
                                                              , typeof(BindableCollectionExt<BridgeTimer>)
                                                              , typeof(TimersPanel)
                                                              , new PropertyMetadata(new BindableCollectionExt<BridgeTimer>(),onBridgeTimersChanged));
        #endregion

        #region Dependency Property ButtonsVisibility
            public Visibility ButtonsVisibility
            {
                get => (Visibility)GetValue(ButtonsVisibilityProperty);
                set => SetValue(ButtonsVisibilityProperty, value);
            }

            public static readonly DependencyProperty ButtonsVisibilityProperty = 
                                   DependencyProperty.Register( nameof(ButtonsVisibility)
                                                              , typeof(Visibility)
                                                              , typeof(TimersPanel)
                                                              , new PropertyMetadata(Visibility.Visible));
        #endregion

        #region Dependency Property CanAndTimer
            public bool CanAndTimer
            {
                get => (bool)GetValue(TimersCanBeAddedProperty);
                set => SetValue(TimersCanBeAddedProperty, value);
            }

            public static readonly DependencyProperty TimersCanBeAddedProperty = 
                                   DependencyProperty.Register( nameof(CanAndTimer)
                                                              , typeof(bool)
                                                              , typeof(TimersPanel)
                                                              , new PropertyMetadata(false,onTimersCanBeAddedChanged ));
        #endregion

        #region Dependency Property Orientation
            public Orientation Orientation
            {
                get => (Orientation)GetValue(OrientationProperty);
                set => SetValue(OrientationProperty, value);
            }

            public static readonly DependencyProperty OrientationProperty = 
                                   DependencyProperty.Register( nameof(Orientation)
                                                              , typeof(Orientation)
                                                              , typeof(TimersPanel)
                                                              , new PropertyMetadata(Orientation.Horizontal, onOrientationChanged));
        #endregion
    #endregion

    #region Private Methods
        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private void Grid_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(typeof(BridgeTimerControl)))
                return;

            var dragged = (BridgeTimerControl)e.Data.GetData(typeof(BridgeTimerControl));

            // Find target
            var pos = e.GetPosition((UIElement)sender);
            var hit = VisualTreeHelper.HitTest(this, pos)?.VisualHit;

            while (hit != null && hit is not BridgeTimerControl)
                hit = VisualTreeHelper.GetParent(hit);

            var target = hit as BridgeTimerControl;

            if (target == null || target == dragged)
                return;

            SwapTimers(dragged.BridgeTimer, target.BridgeTimer);
        }

        private void SwapTimers(BridgeTimer a, BridgeTimer b)
        {
            if (a == null || b == null)
                return;

            var props = GetType().GetProperties()
                                 .Where(p => p.PropertyType == typeof(BridgeTimer))
                                 .ToList();

            PropertyInfo pa = props.First(p => p.GetValue(this) == a);
            PropertyInfo pb = props.First(p => p.GetValue(this) == b);

            pa.SetValue(this, b);
            pb.SetValue(this, a);
        }

        private static void onTimersCanBeAddedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimersPanel ctl)
            {
            }
        }

        private void onCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateBridgeTimers();
        }

        //private void onItemChanged(object sender, ItemPropertyChangedEventArgs<BridgeTimer> e)
        //{
        //    if (e.PropertyName == nameof(Visibility))
        //        UpdateBridgeTimers();
        //}
        private static void onBridgeTimersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimersPanel ctl)
            {
                ctl.BridgeTimers.CollectionChanged+= ctl.onCollectionChanged;
                //ctl.BridgeTimers.ItemChanged      += ctl.onItemChanged;
                ctl.UpdateBridgeTimers();
            }
        }

        private void UpdateBridgeTimers()
        {
            if (Orientation == Orientation.Horizontal)
            {
                BridgeTimer0 = BridgeTimers.Count >  0 ? BridgeTimers[0] : null;
                BridgeTimer1 = BridgeTimers.Count >  1 ? BridgeTimers[1] : null;
                BridgeTimer2 = BridgeTimers.Count >  2 ? BridgeTimers[2] : null;
                BridgeTimer3 = BridgeTimers.Count >  3 ? BridgeTimers[3] : null;
            }
            else
            {
                // In vertical orientation, we might want to display timers differently, but for now, we'll keep it the same.
                BridgeTimer0 = BridgeTimers.Count >  0 ? BridgeTimers[0] : null;
                BridgeTimer2 = BridgeTimers.Count >  1 ? BridgeTimers[1] : null;
                BridgeTimer1 = BridgeTimers.Count >  2 ? BridgeTimers[2] : null;
                BridgeTimer3 = BridgeTimers.Count >  3 ? BridgeTimers[3] : null;
            }
        }

        private static void onOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TimersPanel ctl)
                ctl.UpdateBridgeTimers();
        }

        private void userControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (Design.IsInDesignMode())
            {
                Visibility    = Visibility.Visible;
                Configuration = new Configuration { StartDate = DateTime.Now };
                _             = Configuration.LoadAsync();
            }

            var parent = VisualTreeHelper.GetParent(this);

            while (parent is not null
               &&  parent is not Window)
                parent = VisualTreeHelper.GetParent(parent);

            // Overwrite the default visibility of buttons if the parent is ProjectorView, as it doesn't need them.
            ButtonsVisibility = parent?.GetType().Name == "ProjectorView"
                              ? Visibility.Collapsed
                              : Visibility.Visible;
        }
    #endregion
}
