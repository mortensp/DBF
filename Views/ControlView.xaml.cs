using Syncfusion.UI.Xaml.Grid;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for DbfMembersView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        public ControlView()
        {
            InitializeComponent();

            //this.dgTeams.Loaded += dgStartTeams_Loaded;
        }

        //private void dgStartTeams_Loaded(object sender, System.Windows.RoutedEventArgs e)          => Expand();

        //private void DataGrid_SortColumnsChanged(object sender, GridSortColumnsChangedEventArgs e) => Expand();

        //private void Expand()
        //{
        //    this.dgTeams.Dispatcher.BeginInvoke( System.Windows.Threading.DispatcherPriority.ApplicationIdle
        //                                            , new Action(() =>
        //                                                         {
        //                                                             this.dgTeams.ExpandAllDetailsView();
        //                                                         }));
        //}
    }
}
