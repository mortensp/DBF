using System.Windows;
using DBF.Helpers;
using DBF.ViewModels;
using DBF.Views;
using Microsoft.Xaml.Behaviors;

namespace DBF.HelpSystem;

/// <summary>
/// This class provides attached properties and events to enable window command behaviors (minimize, maximize, close) for buttons in a WPF application and is used 
/// in HelpStyle.xaml. /// 
/// </summary>
public static class HelpWindowCommandsBehavior
{
    #region WindowMaximized Event Attached Property
        /// <summary>
        /// Attached event that fires when the window is maximized or restored.
        /// Use this to react to window state changes.
        /// </summary>
        public static readonly RoutedEvent WindowMaximizedEvent = 
                                           EventManager.RegisterRoutedEvent( "WindowMaximized"
                                                                           , RoutingStrategy.Bubble
                                                                           , typeof(RoutedEventHandler)
                                                                           , typeof(HelpWindowCommandsBehavior));

        public static void AddWindowMaximizedHandler(DependencyObject d, RoutedEventHandler handler) =>
            (d as UIElement)?.AddHandler(WindowMaximizedEvent, handler);

        public static void RemoveWindowMaximizedHandler(DependencyObject d, RoutedEventHandler handler) =>
            (d as UIElement)?.RemoveHandler(WindowMaximizedEvent, handler);
    #endregion

    #region MinimizeCommand Dependency Property
        public static readonly DependencyProperty MinimizeCommandProperty = 
                               DependencyProperty.RegisterAttached( "MinimizeCommand", typeof(bool), typeof(HelpWindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnMinimizeCommandChanged));

        public static void SetMinimizeCommand(DependencyObject d, bool v) => d.SetValue(MinimizeCommandProperty, v);

        public static bool GetMinimizeCommand(DependencyObject d) => (bool)d.GetValue(MinimizeCommandProperty);

        /// <summary>
        /// Attaches a Click event handler to the Button that minimizes its containing Window when the new dependency
        /// property value is true. 
        /// </summary>
        /// <remarks>The handler locates the Button's containing Window via Window.GetWindow and sets its
        /// WindowState to Minimized. A new Click handler is added each time the property changes to true; handlers are not
        /// removed when the value changes to false.</remarks>
        /// <param name="d">The DependencyObject whose property changed; expected to be a Button.</param>
        /// <param name="e">Provides the previous and current values for the dependency property; NewValue is interpreted as a Boolean
        /// indicating whether to attach the Click handler.</param>
        private static void OnMinimizeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var window = Window.GetWindow(btn);

                    if (window != null)
                        window.WindowState = WindowState.Minimized;
                };
        }
    #endregion

    #region MaximizeCommand Dependency Property
        public static readonly DependencyProperty MaximizeCommandProperty = 
                               DependencyProperty.RegisterAttached( "MaximizeCommand", typeof(bool), typeof(HelpWindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnMaximizeCommandChanged));

        public static void SetMaximizeCommand(DependencyObject d, bool v) => d.SetValue(MaximizeCommandProperty, v);

        public static bool GetMaximizeCommand(DependencyObject d) => (bool)d.GetValue(MaximizeCommandProperty);

        /// <summary>
        /// Registers a Click handler on a Button when the MaximizeCommand attached property's value becomes true; the
        /// handler locates the containing Window and toggles its maximized state via IHandleStateChanged.
        /// </summary>
        /// <remarks>Handler is added only when the new value is true. The Click handler retrieves the Window with
        /// Window.GetWindow, checks for IHandleStateChanged, computes the window's maximized bounds and invokes
        /// ToggleMaximize. Note that the implementation attaches a handler without removing prior handlers if the property
        /// changes repeatedly.</remarks>
        /// <param name="d">The DependencyObject whose MaximizeCommand attached property changed; expected to be a Button.</param>
        /// <param name="e">Provides the previous and current values for the property change.</param>
        private static void OnMaximizeCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var  window     = Window.GetWindow(btn);
                    bool isUiThread = Application.Current.Dispatcher.CheckAccess();

                    if (window is IHandleStateChanged handlingWindow)
                    {
                        var max = window.GetMaxSize();
                        handlingWindow.ToggleMaximize(max != new Rect( window.Left
                                                                     , window.Top
                                                                     , window.Width
                                                                     , window.Height));
                    }
                };
        }
    #endregion

    #region CloseCommand Dependency Property
        public static readonly DependencyProperty CloseCommandProperty = 
                               DependencyProperty.RegisterAttached( "CloseCommand", typeof(bool), typeof(HelpWindowCommandsBehavior)
                                                                  , new PropertyMetadata(false, OnCloseCommandChanged));

        public static void SetCloseCommand(DependencyObject d, bool v) => d.SetValue(CloseCommandProperty, v);

        public static bool GetCloseCommand(DependencyObject d) => (bool)d.GetValue(CloseCommandProperty);

        /// <summary>
        /// Attaches a Click event handler to a Button when the dependency property's new value is true; the handler closes
        /// the Button's containing Window.
        /// </summary>
        /// <remarks>The handler is added only when NewValue is true and is not removed when the value becomes
        /// false, which can result in multiple handlers if the property toggles. The handler resolves the containing Window
        /// at click time and safely handles a null Window.</remarks>
        /// <param name="d">The DependencyObject on which the property changed; expected to be a Button instance.</param>
        /// <param name="e">Provides the old and new values of the dependency property; a Click handler is attached when NewValue is true.</param>
        private static void OnCloseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button btn && (bool)e.NewValue)
                btn.Click += (s, args) =>
                {
                    var window = Window.GetWindow(btn);
                    window?.Close();
                };
        }
    #endregion
}

