using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for StartListControl.xaml
    /// </summary>
    public partial class StartListControl : UserControl
    {
        private bool            parentIsViewbox = false;
        //private bool            collapsGroups   = false;
        private int             groupNo         = -1;
        private int             pairGroups      = 0;
        private int             teamGroups      = 0;
        private int pairRows = 0;
        private int teamRows = 0;
        private int maxRows = Properties.Settings.Default.MaxRows;
        private DispatcherTimer groupTimer;

        public StartListControl()
        {
            InitializeComponent();

            this.Loaded                                  += UserControl_Loaded;
            dgPairs.ItemsSourceChanged += (s, e) => setupPaging();
            dgTeams.ItemsSourceChanged += (s, e) => setupPaging();
            dgPairs.Columns["PairName"].ColumnSizer  = GridLengthUnitType.SizeToCells;
            dgTeams.Columns["Names"].ColumnSizer     = GridLengthUnitType.SizeToCells;

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
