using System.Collections.ObjectModel;
using Caliburn.Micro;
using DBF.ViewModels;
using PrintWPF;


namespace DBF.Views;

public partial class TestReport4View: IWpfReport
{
    public TestReport4View()
    {
        InitializeComponent();

        DataContext = IoC.Get<TestReport4ViewModel>();

        //for (var i = 1; i<=1000; i++)
        //    ReportList.Items.Add(new ReportDataRow(i, $"Row {i}", $"This is the description of row number {i}"));
    }
}

