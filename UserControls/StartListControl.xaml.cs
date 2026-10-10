using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

using Caliburn.Micro;
using DBF.DataModel;
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

                setupPaging();

                // Auto-size non-wrapping columns to fit their largest cell (run after record is present)
                AutoSizeColumnsToContent(dg);
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

                setupPaging();

                // Auto-size non-wrapping columns to fit their largest cell (run after record is present)
                AutoSizeColumnsToContent(dg);
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
        private void SfDataGrid_QueryRowHeight(object sender, QueryRowHeightEventArgs e)
        {
            var dataGrid = sender as SfDataGrid;

            if (dataGrid == null)
                return;

            // Fetch visual row
            var recordEntry = dataGrid.GetNodeEntry(e.RowIndex);

            if (recordEntry is null
            ||  recordEntry.IsGroups)
                return;

            var record = dataGrid.GetRecordAtRowIndex(e.RowIndex);

            if (record == null)
                return;

            double maxHeight = dataGrid.RowHeight;

            // Get device DPI -> pixelsPerDip
            var    dpi          = VisualTreeHelper.GetDpi(dataGrid);
            double pixelsPerDip = dpi.PixelsPerDip;

            foreach (var col in dataGrid.Columns.OfType<GridTextColumn>().Where(c => c.TextWrapping == TextWrapping.Wrap))
            {
                var value = record.GetType().GetProperty(col.MappingName)?.GetValue(record)?.ToString();

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

                // Consider column padding when measuring. Use horizontal padding to compute available
                // text width, and vertical padding when adding to the required row height.
                var horizontalPadding = col.Padding.Left + col.Padding.Right;
                var verticalPadding   = col.Padding.Top + col.Padding.Bottom + 4; // small extra gap

                // If this control is placed inside a Viewbox it will be scaled. In that case we must
                // measure the text as it will appear visually (scaled) and then convert the measured
                // visible height back to the unscaled row height that SfDataGrid expects.
                double scaleX = 1.0, scaleY = 1.0;
                try
                {
                    var wnd = Window.GetWindow(dataGrid);

                    if (wnd is not null)
                    {
                        var gt = dataGrid.TransformToVisual(wnd);
                        var p0 = gt.Transform(new Point(0, 0));
                        var pX = gt.Transform(new Point(1, 0));
                        var pY = gt.Transform(new Point(0, 1));
                        scaleX = Math.Abs(pX.X - p0.X);
                        scaleY = Math.Abs(pY.Y - p0.Y);

                        if (scaleX <= 0)
                            scaleX =  1.0;

                        if (scaleY <= 0)
                            scaleY =  1.0;
                    }
                }

                catch
                {
                    // fall back to 1.0 if transform not available
                    scaleX = scaleY = 1.0;
                }

                var availableTextWidth = Math.Max(1.0, (col.ActualWidth - horizontalPadding) * scaleX);

                // Measure text at the scaled font size so FormattedText.Height represents visible height
                var scaledTypeface = typeface;
                var scaledFontSize = fontSize * scaleY;

                var visFormatted = new FormattedText(formattedText.Text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, scaledTypeface, scaledFontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip)
                                   {
                                       MaxTextWidth = availableTextWidth
                                   };

                // convert visible height back to unscaled height used by SfDataGrid
                var requiredUnscaledHeight = (visFormatted.Height / scaleY);

                if (requiredUnscaledHeight + verticalPadding >  maxHeight)
                    maxHeight = Math.Max(maxHeight, requiredUnscaledHeight + verticalPadding);
            }

            e.Height  = maxHeight;
            e.Handled = true;
        }

        private void AutoSizeColumnsToContent(SfDataGrid dg)
        {
            var    dpi          = VisualTreeHelper.GetDpi(dg);
            double pixelsPerDip = dpi.PixelsPerDip;
            var    typeface     = new Typeface(dg.FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
            var    items        = ((ListCollectionView)dg.ItemsSource).SourceCollection;

            foreach (var col in dg.Columns.OfType<GridTextColumn>().Where(c => c.TextWrapping != TextWrapping.Wrap))
            {
                double max = 0;

                // header
                var header   = (col.HeaderText ?? string.Empty);
                var ftHeader = new FormattedText(header, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, dg.FontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip);
                max          = ftHeader.Width;

                // cells
                foreach (var item in items)
                {
                    var v = item?.GetType().GetProperty(col.MappingName)?.GetValue(item)?.ToString();

                    if (string.IsNullOrEmpty(v))
                        continue;

                    var ft = new FormattedText(v, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, dg.FontSize, Brushes.Black, new NumberSubstitution(), TextFormattingMode.Display, pixelsPerDip);
                    max    = Math.Max(max, ft.Width);
                }

                var horizPadding = col.Padding.Left + col.Padding.Right + 8;

                // gap for sorting icon etc.
                col.Width = max + horizPadding;
            }

            dg.UpdateLayout();
        }
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
