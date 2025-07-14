using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Syncfusion.UI.Xaml.Grid;

namespace DBF.UserControls
{
    /// <summary>
    /// Interaction logic for StartListControl.xaml
    /// </summary>
    public partial class StartListControl : UserControl
    {
        public StartListControl()
        {
            InitializeComponent();
            dgStartPairs.Columns["PairName"].ColumnSizer = GridLengthUnitType.SizeToCells;
            dgStartTeams.Columns["Names"].ColumnSizer = GridLengthUnitType.SizeToCells;


            this.dgStartTeams.Loaded += dgStartTeams_Loaded;
        }

        private void dgStartTeams_Loaded(object sender, System.Windows.RoutedEventArgs e) => Exnand();

        private void DataGrid_SortColumnsChanged(object sender, GridSortColumnsChangedEventArgs e) => Exnand();

        private void Exnand()
        {
            this.dgStartTeams.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle
                                                    , new Action(() =>
                                                    {
                                                        this.dgStartTeams.ExpandAllDetailsView();
                                                    }));
        }
    }
}
