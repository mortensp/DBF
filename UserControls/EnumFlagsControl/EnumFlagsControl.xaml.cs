using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using PropertyChanged;

namespace DBF.UserControls
{
    public partial class EnumFlagsControl : UserControl, INotifyPropertyChanged
    {
        #region Constructors
            public EnumFlagsControl()
            {
                InitializeComponent();
                //DataContext = this;
            }
        #endregion

        public ObservableCollection<SelectableFlag> Flags { get; } = new();

        public static readonly DependencyProperty EnumTypeProperty = 
                               DependencyProperty.Register( nameof(               EnumType), typeof(Type), typeof(EnumFlagsControl)
                                                          , new PropertyMetadata(null,     OnEnumTypeChanged));

    

        public Type EnumType
        {
            get=> (Type)GetValue(EnumTypeProperty);
            set=> SetValue(EnumTypeProperty, value);
        }

        public static readonly DependencyProperty SelectedFlagsProperty = 
                               DependencyProperty.Register( nameof(                        SelectedFlags), typeof(Enum), typeof(EnumFlagsControl)
                                                          , new FrameworkPropertyMetadata(null,          FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFlagsChanged));

        public Enum SelectedFlags
        {
            get=> (Enum)GetValue(SelectedFlagsProperty);
            set=> SetValue(SelectedFlagsProperty, value);
        }

        public static readonly DependencyProperty OrientationProperty = 
                               DependencyProperty.Register( nameof(               Orientation), typeof(Orientation), typeof(EnumFlagsControl)
                                                          , new PropertyMetadata(Orientation.Horizontal));

        public Orientation Orientation
        {
            get=> (Orientation)GetValue(OrientationProperty);
            set=> SetValue(OrientationProperty, value);
        }

        [SuppressPropertyChangedWarnings]
        private static void OnEnumTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EnumFlagsControl ctrl)
            {
                ctrl.Flags.Clear();

                if (e.NewValue is Type enumType && enumType.IsEnum)
                    foreach (Enum value in Enum.GetValues(enumType))
                    {
                        if (Convert.ToInt32(value) != 0)
                        {
                            string name = GetEnumDescription(value);
                            ctrl.Flags.Add(new SelectableFlag(name, value, ctrl));
                        }
                    }
            }
        }

        [SuppressPropertyChangedWarnings]
        private static void OnSelectedFlagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is EnumFlagsControl ctrl)
            {
                ctrl._suppressFlagUpdate = true;
                ctrl.UpdateCheckboxesFromFlags();
                ctrl._suppressFlagUpdate = false;
            }
        }

        private bool _suppressFlagUpdate = false;

        internal void UpdateSelectedFlags(Enum flag, bool isSelected)
        {
            if (_suppressFlagUpdate) return;
            int current   = Convert.ToInt32(  SelectedFlags ?? Enum.ToObject(EnumType, 0));
            int flagValue = Convert.ToInt32(flag);

            int updated = isSelected ? (current | flagValue) : (current & ~flagValue);

            if (updated != current)
            {
                _suppressFlagUpdate = true;
                SelectedFlags       = (Enum)Enum.ToObject(EnumType, updated);
                _suppressFlagUpdate = false;
            }
        }

        private void UpdateCheckboxesFromFlags()
        {
            if (SelectedFlags == null) return;
            foreach (var f in Flags)
                f.IsSelected = SelectedFlags.HasFlag(f.Value);
        }

        private static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr  = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
