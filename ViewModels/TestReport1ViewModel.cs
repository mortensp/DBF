using System.Collections.ObjectModel;

namespace DBF.ViewModels;

public class TestReport1ViewModel : Screen
{
    //public UserControl                           Content    { get; set; }
    public ObservableCollection<ReportDataRow> ReportList { get; set; } = new();

    public TestReport1ViewModel()
    {
        for (var i = 1; i <= 90; i++)
            ReportList.Add(new ReportDataRow(i, $"Row {i}", $"This is the description of row number {i}", i / 14));
    }
}

public class TestReport1bViewModel : TestReport1ViewModel { }

