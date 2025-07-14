using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DBF.DataModel;
using Syncfusion.UI.Xaml.Diagram;

namespace DBF.UserControls
{
    public partial class TimersPanel : UserControl
    {
        public TimersPanel()
        {
            InitializeComponent();
        }

        #region Dependency Properties
            #region Dependency Property TimersProperty
                public ObservableCollection<BridgeTimer> Timers
                {
                    get => (ObservableCollection<BridgeTimer>)GetValue(TimersProperty);
                    set => SetValue(TimersProperty, value);
                }

                public static readonly DependencyProperty TimersProperty = 
                                       DependencyProperty.Register( nameof(Timers)
                                                                  , typeof(ObservableCollection<BridgeTimer>)
                                                                  , typeof(TimersPanel));
            #endregion

            #region Dependency Property ButtonsVisibility
                public Visibility ButtonsVisibility
                {
                    get => (Visibility)GetValue(ButtonsVisibilityProperty);
                    set => SetValue(ButtonsVisibilityProperty, value);
                }

                public static readonly DependencyProperty ButtonsVisibilityProperty = 
                                       DependencyProperty.Register( nameof(ButtonsVisibility)
                                                                  , typeof(Visibility)
                                                                  , typeof(TimersPanel)
                                                                  , new PropertyMetadata(Visibility.Visible));
            #endregion

            #region Dependency Property TimersCanBeAddedProperty
                public bool TimersCanBeAdded
                {
                    get => (bool)GetValue(TimersCanBeAddedProperty);
                    set => SetValue(TimersCanBeAddedProperty, value);
                }

                public static readonly DependencyProperty TimersCanBeAddedProperty = 
                                       DependencyProperty.Register( nameof(TimersCanBeAdded)
                                                                  , typeof(bool)
                                                                  , typeof(TimersPanel)
                                                                  , new PropertyMetadata(true));
        #endregion
        #endregion

        private DBF.DataModel.Configuration configuration;
        public DBF.DataModel.Configuration Configuration { get => configuration ?? (configuration = IoC.Get<DBF.DataModel.Configuration>()); private set => configuration = value; }

    }
}