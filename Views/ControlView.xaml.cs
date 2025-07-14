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

            //this.dgStartTeams.Loaded += dgStartTeams_Loaded;
        }

        //private void dgStartTeams_Loaded(object sender, System.Windows.RoutedEventArgs e)          => Exnand();

        //private void DataGrid_SortColumnsChanged(object sender, GridSortColumnsChangedEventArgs e) => Exnand();

        //private void Exnand()
        //{
        //    this.dgStartTeams.Dispatcher.BeginInvoke( System.Windows.Threading.DispatcherPriority.ApplicationIdle
        //                                            , new Action(() =>
        //                                                         {
        //                                                             this.dgStartTeams.ExpandAllDetailsView();
        //                                                         }));
        //}
    }
}
