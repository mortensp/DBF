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
        private int maxRows = Properties.Settings.Default.MaxRows;
        private DispatcherTimer groupTimer;

        public StartListControl()
        {
            InitializeComponent();

            this.Loaded                                  += UserControl_Loaded;
            dgPairs.ItemsSourceChanged              += (s, e) => pagingSetup();
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

            pagingSetup();
        }

        private void pagingSetup()
        {
            if (parentIsViewbox)
            {
                //collapsGroups = false;
                groupNo       = -1;

                if (dgPairs.View.Records.Count >  maxRows)
                {
                    dgPairs.CollapseAllGroup();
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
                dgPairs.CollapseGroup(dgPairs.View.Groups[groupNo] as Group);

            if (++groupNo >= dgPairs.View.Groups.Count)
                groupNo = 0;

            dgPairs.ExpandGroup(dgPairs.View.Groups[groupNo] as Group);
        }
    }
}
