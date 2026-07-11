using System.Collections.ObjectModel;
using System.Windows;
using Syncfusion.Data;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.Helpers;
/// <summary>
/// Syncronizes the state of multiple SfDataGrids based on a shared GroupKey. This includes sorting, grouping, and filtering.
/// </summary>
/// <Usage>
///  help:GridStateSyncBehavior.GroupKey="MainGroup"
/// </Usage>    
public static class GridStateSyncBehavior
{
    private class GridState
    {
        public ObservableCollection<SortColumnDescription>  Sort    { get; } = new();
        public ObservableCollection<GroupColumnDescription> Group   { get; } = new();
        public ObservableCollection<FilterPredicate>        Filters { get; } = new();
    }

    private static readonly Dictionary<string, GridState> Groups = new();

    public static readonly DependencyProperty GroupKeyProperty = 
                           DependencyProperty.RegisterAttached(
                                                                "GroupKey"
                                                              , typeof(string)
                                                              , typeof(GridStateSyncBehavior)
                                                              , new PropertyMetadata(null, OnGroupKeyChanged));

    public static void SetGroupKey(DependencyObject obj, string value)
            => obj.SetValue(GroupKeyProperty, value);

    public static string GetGroupKey(DependencyObject obj)
            => (string)obj.GetValue(GroupKeyProperty);

    private static void OnGroupKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not SfDataGrid grid)
            return;

        if (e.NewValue is not string key || string.IsNullOrWhiteSpace(key))
            return;

        if (!Groups.TryGetValue(key, out var state))
        {
            state       = new GridState();
            Groups[key] = state;
        }

        HookGridEvents(grid, state);
        ApplyFullState(grid, state);
    }

    private static void HookGridEvents(SfDataGrid grid, GridState state)
    {
        // SORT
        grid.SortColumnsChanged+= (_, __) =>
        {
            state.Sort.Clear();

            foreach (var s in grid.SortColumnDescriptions)
                state.Sort.Add(new SortColumnDescription { ColumnName = s.ColumnName, SortDirection = s.SortDirection });
        };

        // GROUP
        grid.GroupColumnDescriptions.CollectionChanged+= (_, __) =>
        {
            state.Group.Clear();

            foreach (var g in grid.GroupColumnDescriptions)
                state.Group.Add(new GroupColumnDescription { ColumnName = g.ColumnName });
        };

        // FILTER
        grid.FilterChanged+= (_, args) =>
        {
            state.Filters.Clear();
            var filters = grid.View.FilterPredicates;

            foreach (var f in filters)
                state.Filters.Add((FilterPredicate)f);
        };
    }

    private static void ApplyFullState(SfDataGrid grid, GridState state)
    {
        // SORT
        grid.SortColumnDescriptions.Clear();

        foreach (var s in state.Sort)
            grid.SortColumnDescriptions.Add(new SortColumnDescription { ColumnName = s.ColumnName, SortDirection = s.SortDirection });

        // GROUP
        grid.GroupColumnDescriptions.Clear();

        foreach (var g in state.Group)
            grid.GroupColumnDescriptions.Add(new GroupColumnDescription { ColumnName = g.ColumnName });

        // FILTER
        if (grid.View is not null)
        {
            grid.View.FilterPredicates.Clear();

            foreach (var f in state.Filters)
                grid.View.FilterPredicates.Add((IFilterDefinition)f);
        }
    }
}
