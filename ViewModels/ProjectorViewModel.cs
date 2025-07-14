using Caliburn.Micro;
using DBF.ViewModels;
using System.Windows.Controls;

namespace DBF.ViewModels
{
    public class ProjectorViewModel : Screen
    {
        public ControlViewModel Control { get; } // Exposed property for binding in ProjectorView

        public ProjectorViewModel(ControlViewModel controlViewModel)
        {            
            Control = controlViewModel;
        }
    }
}