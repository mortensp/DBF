//using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.DataModel;
using Syncfusion.Data;
using Group = Syncfusion.Data.Group;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for ResultsControl.xaml
    /// </summary>
    public partial class ResultsControl : UserControl
    {
        private Configuration config = IoC.Get<Configuration>();

        private bool            parentIsViewbox = false;
        //private bool            collapsGroups   = false;
        private int             groupNo         = -1;
        private int             pairGroups      = 0;
        private int             teamGroups      = 0;
        private int pairRows = 0;
        private int teamRows = 0;
        
        private DispatcherTimer groupTimer;

        public ResultsControl()
        {
            InitializeComponent();

            this.Loaded                += UserControl_Loaded;
            dgPairs.ItemsSourceChanged += (s, e) => setupPaging();
            dgTeams.ItemsSourceChanged += (s, e) => setupPaging();

            // Initialiser timeren
            groupTimer           = new DispatcherTimer();
            groupTimer.Interval  = TimeSpan.FromSeconds(config.ProjectorInterval);
            groupTimer.Tick     += (s, e) => showNextGroup();
            config.PropertyChanged += (s, e) => 
            { 
                groupTimer.Interval = TimeSpan.FromSeconds(config.ProjectorInterval);
                setupPaging();
                };
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

                if (pairRows + teamRows > config.ProjectorMaxRows)
                {
                    dgPairs.CollapseAllGroup();
                    dgTeams.CollapseAllGroup();
                    showNextGroup();
                    groupTimer.Start();
                }
                else
                {
                    dgPairs.ExpandAllGroup();
                    dgTeams.ExpandAllGroup();
                    groupTimer.Stop();
                }
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
