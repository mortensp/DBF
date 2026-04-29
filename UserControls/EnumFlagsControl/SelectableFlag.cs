using System.ComponentModel;
using System.Runtime.CompilerServices;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Providers;

namespace DBF.UserControls
{
    public class SelectableFlag : INotifyPropertyChanged
    {
        private          bool             _isSelected;
        private readonly EnumFlagsControl _parent;

        public string Name  { get; }
        public Enum   Value { get; }

        public bool IsSelected
        {
            get=> _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    _parent.UpdateSelectedFlags(Value, value);
                }
            }
        }

        public SelectableFlag(string name, Enum value, EnumFlagsControl parent)
        {
            Name    = name;
            Value   = value;
            _parent = parent;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propName = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}

