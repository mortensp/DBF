using System.Collections.ObjectModel;

namespace DBF.ViewModels;

public class ReportsViewModel:Screen 
{
    public ReportsViewModel()
    {
        for (var i = 1; i <= 8; i++)
            HorizontalReportList.Add(new ReportDataRow(i, $"Row {i}", $"This is the description of column number {i}", $"Group: {i/3}"));

        for (var i = 1; i <= 63; i++)
            ReportList.Add(new ReportDataRow(i, $"Row {i}", $"This is the description of row number {i}", $"Group: {i/4}"));
    }

    public ObservableCollection<ReportDataRow> HorizontalReportList { get; set; } = new();

    public ObservableCollection<ReportDataRow> ReportList         { get; set; } = new();
}
