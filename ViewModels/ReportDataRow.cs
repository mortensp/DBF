namespace DBF.ViewModels;

public class ReportDataRow
{
    public ReportDataRow(int id, string name, string description,int? group=null)
    {
        Id          = id;
        Name        = name;
        Description = description;
        Group       = group??0;
    }

    public int    Id          { get; }
    public string Name        { get; }
    public string Description { get; }
    
    public int    Group          { get; }
    
}

