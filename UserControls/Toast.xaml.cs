using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DBF.UserControls;

public partial class Toast : UserControl
{
    public Toast(string message)
    {
        InitializeComponent();
        Txt.Text = message;

        Loaded += (_, __) => Animate();
    }

    private void Animate()
    {
        var sb = new Storyboard();

        // Fade in
        sb.Children.Add(new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
        {
            BeginTime = TimeSpan.Zero
        });

        // Fade out
        sb.Children.Add(new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
        {
            BeginTime = TimeSpan.FromSeconds(5.5)
        });

        Storyboard.SetTarget(sb.Children[0], this);
        Storyboard.SetTargetProperty(sb.Children[0], new PropertyPath("Opacity"));

        Storyboard.SetTarget(sb.Children[1], this);
        Storyboard.SetTargetProperty(sb.Children[1], new PropertyPath("Opacity"));

        sb.Completed += (_, __) =>
        {
            (Parent as Panel)?.Children.Remove(this);
        };

        sb.Begin();
    }
}
