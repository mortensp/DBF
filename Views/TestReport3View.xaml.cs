using Caliburn.Micro;
using DBF.ViewModels;
using PrintWPF;

namespace DBF.Views
{
    public partial class TestReport3View : IWpfReport
    {
        public TestReport3View()
        {
            InitializeComponent();

            DataContext = IoC.Get<TestReport3ViewModel>();
        }
    }
}
