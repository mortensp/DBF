using System.Collections.ObjectModel;

namespace DBF.ViewModels;

public class TestReport1ViewModel : Screen
{
    //public UserControl                           Content    { get; set; }

    public ObservableCollection<ReportDataModel> ReportList { get; set; } = new();

    public TestReport1ViewModel()
    {

        for (var i = 1; i <= 200; i++)
            ReportList.Add(new ReportDataModel(i, $"Row {i}", $"This is the description of row number {i}"));
    }
}

public class ReportDataModel
{
    public ReportDataModel(int id, string name, string description)
    {
        Id          = id;
        Name        = name;
        Description = description;
    }

    public int    Id          { get; }
    public string Name        { get; }
    public string Description { get; }
}

