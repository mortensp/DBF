using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.ViewModels;
using DBF.Views;
using PrintWPF;

using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Group = Syncfusion.Data.Group;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for StartListControl.xaml
    /// </summary>
    public partial class StartListControl : UserControl, IWpfReport
    {
        private Configuration     config;
        private bool              inProjectorView;
        private int               linesAllocated;
        private IEnumerable<Pair> pairs;
        private IEnumerable<Team> teams;
        private int               pairRows;
        private int               teamRows;
        private DispatcherTimer   groupTimer =new();

        #region Pagination
            private DataModel.PageRange currentPageRange = new(0, 0);

            private int pageIndex = -1;

            private List<DataModel.PageRange> pageRanges = [];
        #endregion

        #region Constructors
            public StartListControl()
            {
                InitializeComponent();
                this.DataContextChanged+= StartlistControl_DataContextChanged;
                this.Loaded            += UserControl_Loaded;

                // Pairs
                dgPairs.Columns["PairName"].ColumnSizer = GridLengthUnitType.SizeToCells;
                dgPairs.GroupCollapsed                 += (s, e) => dgPairs.RefreshSorting();
                dgPairs.GroupExpanded                  += (s, e) => dgPairs.RefreshSorting();
                dgPairs.GroupCollapsing                += (s, e) => { e.Cancel = inProjectorView; };
                dgPairs.GroupExpanding                 += (s, e) => { e.Cancel = inProjectorView; };
                dgPairs.ItemsSourceChanged             += onPairsChanged;

                // Teams                
                dgTeams.Columns["Names"].ColumnSizer = GridLengthUnitType.SizeToCells;
                dgTeams.GroupCollapsed              += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupExpanded               += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupCollapsing             += (s, e) => { e.Cancel = inProjectorView; };
                dgTeams.GroupExpanding              += (s, e) => { e.Cancel = inProjectorView; };
                dgTeams.ItemsSourceChanged          += onTeamsChanged;

                config ??= IoC.Get<Configuration>();
            }
        #endregion

        //public double HeaderHeight { get; set; } = 82;
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this);

            while (parent is not null
               &&  parent is not Window)
                parent = VisualTreeHelper.GetParent(parent);

            inProjectorView = parent?.GetType().Name == "ProjectorView";

            setupPaging();

            groupTimer.Tick       += (s, e) => showNextGroup();
            config.PropertyChanged+= (s, e) =>
            {
                if (e.PropertyName == nameof(Configuration.ProjectorMaxRows))
                    setupPaging();
            };
        }

        private void onPairsChanged(object s, GridItemsSourceChangedEventArgs e)
        {
            if (s              is SfDataGrid dg
            &&  dg.ItemsSource is not null)
            {
                dg.ClearFilters();

                pairs = ((ListCollectionView)dg.ItemsSource).SourceCollection as IEnumerable<Pair>;

                var subGroup      = dg.GroupColumnDescriptions.FirstOrDefault(g => g.ColumnName == "SubGroup");
                var showSubGroups = pairs.Any(p => !string.IsNullOrEmpty(p.SubGroup));

                if (subGroup is null && showSubGroups)
                    dg.GroupColumnDescriptions.Add(new GroupColumnDescription() { ColumnName = "SubGroup" });

                if (subGroup is not null && !showSubGroups)
                    dg.GroupColumnDescriptions.Remove(subGroup);

                dg.View.Filter = item =>  pageRanges.Count == 0
                                      ||  pageIndex        <  0
                                      ||  item             is Pair pair
                                      &&  pageRanges[pageIndex].Contains(pair.EntryNo);
                dg.View.RefreshFilter();

                if (pairs.Count() >  0)
                {
                    setupPaging();

                    // Auto-size non-wrapping columns to fit their largest cell (run after record is present)
                    AutoSizeColumnsToContent(dg);
                }
            }
        }

        private void onTeamsChanged(object s, GridItemsSourceChangedEventArgs e)
        {
            if (s              is SfDataGrid dg
            &&  dg.ItemsSource is not null)
            {
                dg.ClearFilters();

                teams = ((ListCollectionView)dg.ItemsSource).SourceCollection as IEnumerable<Team>;

                dg.View.Filter = item =>  pageRanges.Count == 0
                                      ||  pageIndex        <  0
                                      ||  item             is Team team
                                      &&  pageRanges[pageIndex].Contains(team.EntryNo);
                dg.View.RefreshFilter();

                if (teams.Count() >  0)
                {
                    setupPaging();

                    // Auto-size non-wrapping columns to fit their largest cell (run after record is present)
                    AutoSizeColumnsToContent(dg);
                }
            }
        }

        private void setupPaging()
        {
            if (inProjectorView)
            {
                groupTimer.Stop();
                groupTimer.Interval = TimeSpan.FromSeconds(config.ProjectorInterval);

                pageRanges       = [];
                pageIndex        = -1;
                linesAllocated   = 0;
                currentPageRange = new(0, 0);
                pairRows         = pairs?.Count() ?? 0;
                teamRows         = teams?.Count() ?? 0;

                dgPairs.View?.RefreshFilter();
                dgTeams.View?.RefreshFilter();

                if (pairRows + teamRows >  0)
                {
                    parse(dgPairs);
                    parse(dgTeams);

                    if (pageRanges.Count == 0)
                        pageRanges = [new DataModel.PageRange(0, pairRows + teamRows)];
                    else
                    {
                        showNextGroup();
                        groupTimer.Start();
                    }
                }
            }
        }

        #region Column and Row Sizing
            private void QueryRowHeight(object sender, QueryRowHeightEventArgs e) => SfGridHelper.QueryRowHeight(sender, e);

            private void AutoSizeColumnsToContent(SfDataGrid dg) => SfGridHelper.AutoSizeTextColumns(dg);
        #endregion

        #region Display Line Groups
            private void parse(SfDataGrid dataGrid)
            {
                if (dataGrid?.View              is null
                ||  dataGrid.View.Records.Count == 0)
                    return;

                if (dataGrid.View.Groups       is null
                ||  dataGrid.View.Groups.Count == 0)
                    parse(dataGrid.View.Records);
                else
                    foreach (Group group in dataGrid.View.Groups)
                        parse(group);

                if (!currentPageRange.IsEmpty)
                    endInterval();
            }

            private void parse(Group group)
            {
                linesAllocated++;  // allocate one line for the group header

                if (group.Groups is null)
                {
                    parse(group.Records);
                    return;
                }

                // Foreach will not work here
                for (int i = 0; i <  group.Groups.Count(); i++)
                {
                    var subGroup =group.Groups[i];

                    if (subGroup.Count() == 0)
                    {
                        linesAllocated++;  // allocate one line for the group header
                        parse(subGroup.Records);
                    }
                    else
                        parse(subGroup);
                }
            }

            private void parse(ICollection<Syncfusion.Data.RecordEntry> records)
            {
                var linesNeeded = records.Count;

                if (!currentPageRange.IsEmpty
                &&  records.Count >  config.ProjectorMaxRows - linesAllocated)
                    endInterval();

                if (records.Count <= config.ProjectorMaxRows - linesAllocated)
                {
                    currentPageRange.To+= records.Count;
                    linesAllocated     += records.Count;
                }
                else
                    while (linesNeeded >  0)
                    {
                        var cnt             = Math.Min(linesNeeded, config.ProjectorMaxRows - linesAllocated);
                        currentPageRange.To+= cnt;
                        linesAllocated     += cnt;
                        linesNeeded        -= cnt;

                        if (linesAllocated >= config.ProjectorMaxRows)
                        {
                            endInterval();
                            currentPageRange = new DataModel.PageRange(currentPageRange.To, currentPageRange.To);
                            linesAllocated   = 0;
                        }
                    }
            }

            public void endInterval()
            {
                if (!currentPageRange.IsEmpty)
                    pageRanges.Add(currentPageRange);

                currentPageRange = new DataModel.PageRange(currentPageRange.To, currentPageRange.To);
                linesAllocated   = 0;
            }

            private void showNextGroup()
            {
                if (++pageIndex >= pageRanges.Count)
                    pageIndex = 0;

                dgPairs.View?.RefreshFilter();
                dgTeams.View?.RefreshFilter();
            }
        #endregion

        #region OnDatacontextChanged
            private void StartlistControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                UpdateColumnVisibility();

                if (e.NewValue is ControlViewModel vm)
                    vm.PropertyChanged += ViewModel_PropertyChanged;

                if (e.OldValue is ControlViewModel oldVm)
                    oldVm.PropertyChanged -= ViewModel_PropertyChanged;
            }

            private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                if (e.PropertyName is  nameof(ControlViewModel.HideHac)
                                   or  nameof(ControlViewModel.HideHacGrp)
                                   or  nameof(ControlViewModel.ShowAsOneGroup)
                                   or  nameof(ControlViewModel.HideTournamentSummery)
                                   or  nameof(ControlViewModel.Pairs)
                                   or  nameof(ControlViewModel.Teams))
                    UpdateColumnVisibility();
            }

            private void UpdateColumnVisibility()
            {
                if (this.DataContext is ControlViewModel vm)
                {
                    // Hide or unhide columns
                    dgPairs.Columns["HACRankSectionGroup"]?.IsHidden = vm.HideHacGrp;
                    dgPairs.Columns["ExpectedPct"]?.IsHidden = vm.ImpsPair || vm.HideHac;
                    dgPairs.Columns["HACTotal"]?.IsHidden = vm.HideTournamentSummery || vm.HideHac;
                    dgPairs.Columns["TournamentRank"]?.IsHidden = vm.HideTournamentSummery;
                    dgPairs.Columns["TournamentResult"]?.IsHidden = vm.HideTournamentSummery;
                }
            }
        #endregion
    }
}
