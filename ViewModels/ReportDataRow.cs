namespace DBF.ViewModels;

public class ReportDataRow
{
    public ReportDataRow(int id, string name, string description,string group=null)
    {
        Id          = id;
        Name        = name;
        Description = description;
        Group       = group;
    }

    public int    Id          { get; }
    public string Name        { get; }
    public string Description { get; }
    
    public string    Group          { get; }
    
}

