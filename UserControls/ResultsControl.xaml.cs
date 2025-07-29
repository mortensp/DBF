using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Syncfusion.Data;
using Syncfusion.PivotAnalysis.Base;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsControl.xaml
    /// </summary>
    public partial class ResultsControl : UserControl
    {
        private bool            parentIsViewbox = false;
        //private bool            collapsGroups   = false;
        private int             groupNo         = -1;
        private int             pairGroups      = 0;
        private int             teamGroups      = 0;
        private int pairRows = 0;
        private int teamRows = 0;
        private int             maxRows         = Properties.Settings.Default.MaxRows;
        private DispatcherTimer groupTimer;

        public ResultsControl()
        {
            InitializeComponent();

            this.Loaded                += UserControl_Loaded;
            dgPairs.ItemsSourceChanged += (s, e) => setupPaging();
            dgTeams.ItemsSourceChanged += (s, e) => setupPaging();

            // Initialiser timeren
            groupTimer           = new DispatcherTimer();
            groupTimer.Interval  = TimeSpan.FromSeconds(Properties.Settings.Default.DisplaySecounds);
            groupTimer.Tick     += (s, e) => showNextGroup();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);

            while (parent is not null
               &&  parent is not Viewbox)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is not null)
                parentIsViewbox = true;

            setupPaging();
        }

        private void setupPaging()
        {
            if (parentIsViewbox)
            {
                //collapsGroups = false;
                groupNo       = -1;
                pairGroups    = dgPairs.View?.Groups?.Count ?? 0;
                teamGroups    = dgTeams.View?.Groups?.Count ?? 0;
                pairRows = dgPairs.View?.Records.Count ?? 0;
                teamRows = dgTeams.View?.Records.Count ?? 0;

                if (pairRows + teamRows>  maxRows)
                {
                    dgPairs.CollapseAllGroup();
                    dgTeams.CollapseAllGroup();
                    showNextGroup();
                    groupTimer.Start();
                }
                else
                    groupTimer.Stop();
            }
        }

        private void showNextGroup()
        {
            if (groupNo >  -1)
                if (groupNo <  pairGroups)
                    dgPairs.CollapseGroup(dgPairs.View.Groups[groupNo] as Group);
                else
                    dgTeams.CollapseGroup(dgTeams.View.Groups[groupNo - pairGroups] as Group);

            if (++groupNo >= pairGroups + teamGroups)
                groupNo = 0;

            if (groupNo <  pairGroups)
                dgPairs.ExpandGroup(dgPairs.View.Groups[groupNo] as Group);
            else
                dgTeams.ExpandGroup(dgTeams.View.Groups[groupNo - pairGroups] as Group);
        }
    }
}
