using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.ViewModels;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.Windows.Controls.PivotGrid;
using Group = Syncfusion.Data.Group;

namespace DBF.UserControls
{
    public partial class ResultsControl : UserControl, INotifyPropertyChanged
    {
        private Configuration config = IoC.Get<Configuration>();

        private int               displayLineIndex = -1;
        private Interval          interval         = new(0, 0);
        private int               linesAllocated;
        private int               linesNeeded;
        private IEnumerable<Pair> pairs;
        private IEnumerable<Team> teams;
        private int               pairRows;
        private int               teamRows;
        private List<Interval>    displayLines     = [];
        private DispatcherTimer   groupTimer;

        #region Constructors
            public ResultsControl()
            {
                InitializeComponent();
                this.DataContextChanged+= ResultsControl_DataContextChanged;
                this.Loaded            += UserControl_Loaded;

                dgPairs.Columns["PairName"].ColumnSizer = GridLengthUnitType.SizeToCells;
                dgPairs.GroupCollapsed                 += (s, e) => dgPairs.RefreshSorting();
                dgPairs.GroupExpanded                  += (s, e) => dgPairs.RefreshSorting();
                dgPairs.GroupCollapsing                += (s, e) => { e.Cancel = InProjectorView; };
                dgPairs.GroupExpanding                 += (s, e) => { e.Cancel = InProjectorView; };
                dgPairs.ItemsSourceChanged             += onPairsChanged;

                dgTeams.Columns["TeamName"].ColumnSizer = GridLengthUnitType.SizeToCells;
                dgTeams.GroupCollapsed                 += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupExpanded                  += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupCollapsing                += (s, e) => { e.Cancel = InProjectorView; };
                dgTeams.GroupExpanding                 += (s, e) => { e.Cancel = InProjectorView; };
                dgTeams.ItemsSourceChanged             += onTeamsChanged;

                // Initialiser timeren
                groupTimer             = new DispatcherTimer();
                groupTimer.Tick       += (s, e) => showNextGroup();
                config.PropertyChanged+= (s, e) => setupPaging();

                //showBreak();
            }
        #endregion

        #region Public Properties
            public Visibility CollapsedInProjectorView => InProjectorView
                                                      ||  DataContext is not ControlViewModel vm
                                                        ? Visibility.Collapsed
                                                        : vm.ShowAsOneGroupVisibility;

            public bool       InProjectorView          { get; private set; }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);

            while (parent is not null
               &&  parent is not Window)
                parent = VisualTreeHelper.GetParent(parent);

            InProjectorView = parent?.GetType().Name == "ProjectorView";
            setupPaging();
        }

        private void onPairsChanged(object s, GridItemsSourceChangedEventArgs e)
        {
            if (s              is SfDataGrid dg
            &&  dg.ItemsSource is not null)
            {
                try
                {
                    dg.ClearFilters();
                    dg.GroupColumnDescriptions.Clear();
                    dg.GroupColumnDescriptions.Add(new GroupColumnDescription() { ColumnName = "Group" });

                    pairs = ((ListCollectionView)dg.ItemsSource).SourceCollection as IEnumerable<Pair>;

                    // SubGroups?                
                    if (pairs.Any(p => !string.IsNullOrEmpty(p.SubGroup)))
                        dg.GroupColumnDescriptions.Add(new GroupColumnDescription() { ColumnName = "SubGroup" });
                }

                catch (Exception ex)
                {
                    Logger.Exception(ex);
                    throw;
                }

                dg.RefreshSorting();
                dg.View.RefreshFilter();
                dg.View.Filter = item =>  displayLines.Count == 0
                                      ||  item               is Pair pair
                                      &&  displayLines[displayLineIndex].Contains(pair.EntryNo);
                // 
                setupPaging();

                //dg.Visibility = pairs.Any(p => p.ResultStr == null) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void onTeamsChanged(object s, GridItemsSourceChangedEventArgs e)
        {
            if (s              is SfDataGrid dg
            &&  dg.ItemsSource is not null)
            {
                dg.ClearFilters();

                dg.GroupColumnDescriptions.Clear();
                dg.GroupColumnDescriptions.Add(new GroupColumnDescription() { ColumnName = "Group" });

                teams = ((ListCollectionView)dg.ItemsSource).SourceCollection as IEnumerable<Team>;

                dg.View.RefreshFilter();
                dg.View.Filter = item =>  displayLines.Count == 0
                                      ||  item               is Team team
                                      &&  displayLines[displayLineIndex].Contains(team.EntryNo);
                // 
                setupPaging();

                //dg.Visibility = teams.Any(p => p.ImpScoreStr == null) ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void setupPaging()
        {
            if (InProjectorView)
            {
                groupTimer.Stop();
                groupTimer.Interval = TimeSpan.FromSeconds(config.ProjectorInterval);

                displayLines     = [];
                displayLineIndex = -1;
                linesAllocated   = 0;
                linesNeeded      = 0;
                interval         = new(0, 0);
                pairRows         = pairs?.Count() ?? 0;
                teamRows         = teams?.Count() ?? 0;

                dgPairs.View?.RefreshFilter();
                dgTeams.View?.RefreshFilter();

                if (pairRows + teamRows >  0)
                {
                    splitDataGrid(dgPairs);
                    splitDataGrid(dgTeams);

                    if (linesNeeded >  config.ProjectorMaxRows)
                    {
                        showNextGroup();
                        groupTimer.Start();
                    }
                    else
                        displayLines = [new Interval(0, pairRows + teamRows)];
                }
            }
        }

        #region Display Line Groups
            private void splitDataGrid(SfDataGrid dataGrid)
            {
                if (dataGrid?.View is null)
                    return;

                foreach (Group group in dataGrid.View.Groups.OrderBy(g => ((Group)g).Key))
                {
                    linesNeeded+= group.LineCount();

                    if (group.LineCount() <= config.ProjectorMaxRows - linesAllocated)
                        addGroup(group);
                    else
                    {
                        if (!interval.IsEmpty)
                            endInterval();

                        if (group.Groups is null || group.Groups.Count == 0)
                            splitGroup(group);
                        else
                            foreach (Group subgroup in group.Groups)
                                if (subgroup.LineCount() <= config.ProjectorMaxRows - linesAllocated)
                                    addGroup(subgroup);
                                else
                                    splitGroup(subgroup);
                    }
                }

                if (!interval.IsEmpty)
                    endInterval();
            }

            private void addGroup(Group group)
            {
                interval.To   += group.Records?.Count() ?? 0;
                linesAllocated+= group.LineCount();
            }

            private void splitGroup(Group group)
            {
                for (int rows = group.Records.Count; rows >  0;)
                {
                    var cnt     = Math.Min(rows, config.ProjectorMaxRows - linesAllocated - 1);
                    interval.To+= cnt;
                    displayLines.Add(interval);
                    interval       = new Interval(interval.To, interval.To);
                    linesAllocated = 0;
                    rows          -= cnt;
                }
            }

            public void endInterval()
            {
                if (interval.To >  interval.From)
                    displayLines.Add(interval);

                interval       = new Interval(interval.To, interval.To);
                linesAllocated = 0;
            }

            private void showNextGroup()
            {
                if (++displayLineIndex >= displayLines.Count)
                    displayLineIndex = 0;

                dgPairs.View?.RefreshFilter();
                dgTeams.View?.RefreshFilter();
            }
        #endregion

        public void btnBreak_Click(object sender, RoutedEventArgs e)
        {
            Debugger.Break();
        }

        #region OnDatacontextChanged
            private void ResultsControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                UpdateColumnVisibility();

                if (e.NewValue is ControlViewModel vm)
                    vm.PropertyChanged += ViewModel_PropertyChanged;

                if (e.OldValue is ControlViewModel oldVm)
                    oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }

            private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(ControlViewModel.HideHac)
                ||  e.PropertyName == nameof(ControlViewModel.HideHacGrp)
                ||  e.PropertyName == nameof(ControlViewModel.ShowAsOneGroup)
                ||  e.PropertyName == nameof(ControlViewModel.HideTournamentSummery))
                    UpdateColumnVisibility();

                if (e.PropertyName == nameof(ControlViewModel.ShowAsOneGroupVisibility))
                    OnPropertyChanged(nameof(CollapsedInProjectorView));  // Notify binding
            }

            private void UpdateColumnVisibility()
            {
                if (this.DataContext is ControlViewModel vm)
                {
                    // dgPairs.Columns[] bruger MappingName som key

                    // HideHacGrp
                    var hacGrpColumn = dgPairs.Columns.FirstOrDefault(c => c.MappingName == "HACRankSectionGroup");

                    if (hacGrpColumn != null)
                        hacGrpColumn.IsHidden = vm.HideHacGrp;

                    // HideTournamentSummery
                    //var tournamentColumns = dgPairs.Columns.Where(c =>  c.MappingName == "TournamentRank"
                    //                                                ||  c.MappingName == "TournamentResult"
                    //                                                ||  c.MappingName == "HACTotal");

                    //foreach (var col in tournamentColumns)
                    //    if (col.MappingName == "HACTotal")
                    //        col.IsHidden = vm.HideTournamentSummery || vm.HideHac;
                    //    else
                    //        col.IsHidden = vm.HideTournamentSummery;
                    dgPairs.Columns["HACTotal"]?.IsHidden = vm.HideTournamentSummery || vm.HideHac;
                    dgPairs.Columns["TournamentRank"]?.IsHidden = vm.HideTournamentSummery;
                    dgPairs.Columns["TournamentResult"]?.IsHidden = vm.HideTournamentSummery;
                }
            }
        #endregion

        private void showBreak()
        {
            try
            {
                // Only show the test button when running under a debugger
                if (Debugger.IsAttached)
                {
                    var btn = this.FindName("btnBreak") as UIElement;

                    if (btn != null)
                        btn.Visibility = Visibility.Visible;
                }
            }

            catch
            {
                // ignore any lookup failures
            }
        }
    }
}
