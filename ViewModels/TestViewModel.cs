using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DBF.DataModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Caliburn.Micro;
using DBF.AudioServices;
using DBF.UserControls;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DBF.ViewModels;

public class TestViewModel : Screen
{
    public TestViewModel()
    {
        _=Configuration.LoadAsync();
        Groups = GroupFlags.A | GroupFlags.C;
    }

    public Configuration Configuration { get; set; } = new();
    public GroupFlags    Groups        { get; set; }
}

