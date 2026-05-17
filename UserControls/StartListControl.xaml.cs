using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Caliburn.Micro;
using DBF.DataModel;
using DBF.Helpers;
using DBF.ViewModels;
using Syncfusion.Data;
using Syncfusion.Data.Extensions;
using Syncfusion.UI.Xaml.Grid;
using Syncfusion.UI.Xaml.Grid.Helpers;
using Group = Syncfusion.Data.Group;
namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for StartListControl.xaml
    /// </summary>
    public partial class StartListControl : UserControl
    {
        private Configuration config;// = IoC.Get<Configuration>();

        private bool              parentIsViewbox;
        private int               displayLineIndex = -1;
        private Interval          interval         = new(0, 0);
        private int               linesAllocated;
        private int               linesNeeded;
        private IEnumerable<Pair> pairs;
        private IEnumerable<Team> teams;
        private int               pairRows;
        private int               teamRows;
        private List<Interval>    displayLines     = [];
        private DispatcherTimer   groupTimer       =new();

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
                dgPairs.GroupCollapsing                += (s, e) => { e.Cancel = parentIsViewbox; };
                dgPairs.GroupExpanding                 += (s, e) => { e.Cancel = parentIsViewbox; };
                dgPairs.ItemsSourceChanged             += onPairsChanged;

                // Teams                
                dgTeams.Columns["Names"].ColumnSizer = GridLengthUnitType.SizeToCells;
                dgTeams.GroupCollapsed              += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupExpanded               += (s, e) => dgTeams.RefreshSorting();
                dgTeams.GroupCollapsing             += (s, e) => { e.Cancel = parentIsViewbox; };
                dgTeams.GroupExpanding              += (s, e) => { e.Cancel = parentIsViewbox; };
                dgTeams.ItemsSourceChanged          += onTeamsChanged;
            }
        #endregion

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            config ??= IoC.Get<Configuration>();

            var parent = VisualTreeHelper.GetParent(this);

            while (parent is not null
               &&  parent is not Viewbox)
                parent = VisualTreeHelper.GetParent(parent);

            if (parent is not null)
                parentIsViewbox = true;

            setupPaging();

            //groupTimer             = new DispatcherTimer();
            groupTimer.Tick       += (s, e) => showNextGroup();
            config.PropertyChanged+= (s, e) => setupPaging();
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

                dg.View.Filter = item =>  displayLines.Count == 0
                                      ||  item               is Pair pair
                                      &&  displayLines[displayLineIndex].Contains(pair.EntryNo);
                dg.View.RefreshFilter();

                setupPaging();
            }
        }

        private void onTeamsChanged(object s, GridItemsSourceChangedEventArgs e)
        {
            if (s              is SfDataGrid dg
            &&  dg.ItemsSource is not null)
            {
                dg.ClearFilters();

                teams = ((ListCollectionView)dg.ItemsSource).SourceCollection as IEnumerable<Team>;

                dg.View.Filter = item =>  displayLines.Count == 0
                                      ||  item               is Team team
                                      &&  displayLines[displayLineIndex].Contains(team.EntryNo);
                dg.View.RefreshFilter();

                // 
                setupPaging();
            }
        }

        private void setupPaging()
        {
            if (parentIsViewbox)
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

        private void SfDataGrid_QueryRowHeight(object sender, QueryRowHeightEventArgs e)
        {
            var dataGrid = sender as SfDataGrid;

            if (dataGrid == null)
                return;

            // Hent visuel række
            var rowGenerator = dataGrid.GetRowGenerator();
            var rowInfo      = rowGenerator.Items[e.RowIndex];

            // Vi håndterer kun data-rækker
            if (rowInfo?.RowData == null || rowInfo.RowData is Group)
                return;

            var data = rowInfo.RowData;

            if (data == null)
                return;

            double maxHeight = dataGrid.RowHeight;

            // Get device DPI -> pixelsPerDip
            var    dpi          = VisualTreeHelper.GetDpi(dataGrid);
            double pixelsPerDip = dpi.PixelsPerDip;

            foreach (var col in dataGrid.Columns.OfType<GridTextColumn>().Where(c => c.TextWrapping == TextWrapping.Wrap))
            {
                var value = data.GetType().GetProperty(col.MappingName)?.GetValue(data)?.ToString();

                if (string.IsNullOrEmpty(value))
                    continue;

                var fontFamily = dataGrid.FontFamily;
                var fontSize   = dataGrid.FontSize;
                var typeface   = new Typeface(fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                // Use overload that accepts pixelsPerDip (DPI aware measurement)
                var formattedText = new FormattedText( value
                                                     , CultureInfo.CurrentCulture
                                                     , FlowDirection.LeftToRight
                                                     , typeface
                                                     , fontSize
                                                     , Brushes.Black
                                                     , new NumberSubstitution()
                                                     , TextFormattingMode.Display
                                                     , pixelsPerDip);

                var padding = col.Padding.Left + col.Padding.Right;

                if (formattedText.Width + padding >  col.ActualWidth)
                {
                    formattedText.MaxTextWidth = col.ActualWidth;
                    maxHeight                  = Math.Max(maxHeight, formattedText.Height + padding);
                }
            }

            e.Height  = maxHeight;
            e.Handled = true;
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
                interval.To   += group.Records?.Count() ?? 32;
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
