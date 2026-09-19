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
using Caliburn.Micro;
using DBF.ViewModels;
using PrintWPF;

namespace DBF.Views
{
    /// <summary>
    /// Interaction logic for TestReport1b.xaml
    /// </summary>
    public partial class TestReport1bView : UserControl, IWpfReport
    {
        public TestReport1bView()
        {
            InitializeComponent();

            DataContext = IoC.Get<TestReport1ViewModel>();
        }
    }
}
