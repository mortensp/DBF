using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Syncfusion.Windows.Controls.Gantt;
using Syncfusion.Windows.Controls.Grid;

namespace DBF.Helpers;

public static class Design
{
    public static bool IsInDesignMode() => (bool)DesignerProperties.IsInDesignModeProperty
                                                                   .GetMetadata(typeof(DependencyObject))
                                                                   .DefaultValue;

    public static bool IsInDesignMode(this DependencyObject dep) => (bool)DesignerProperties.GetIsInDesignMode(dep);
}

