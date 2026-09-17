
using Caliburn.Micro;
using DBF.ViewModels;
using PrintWPF;

namespace DBF.Views;

public partial class TestReport1View: IWpfReport
{
    public TestReport1View()
    {
        InitializeComponent();

        DataContext = IoC.Get<TestReport1ViewModel>();
    }
}
