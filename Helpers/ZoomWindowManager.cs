using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Caliburn.Micro;

namespace DBF.Helpers;

public class ZoomWindowManager : WindowManager
{

    protected override async Task<Window> CreateWindowAsync(object rootModel, bool isDialog, object context, IDictionary<string, object> settings)
    {
        // Let Caliburn create the window first
        var window = await base.CreateWindowAsync(rootModel, isDialog, context, settings);

        // Enable zoom on the window root
        ZoomBehavior.SetEnableZoom(window, true);

        window.Closed+= (s, e) => ZoomBehavior.SetEnableZoom(window, false);

        return window;
    }
}

