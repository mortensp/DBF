global using System;
global using System.Text;
global using System.Diagnostics;
global using DBF.Localization;
global using Lex = DBF.Localization.Strings;

// the rest is to hide Windows.Forms which is only here for sfBadge and
// as we uses Forms by <UseWindowsForms>true</UseWindowsForms> in the csproj file
global using System.Windows.Media;
global using Application = System.Windows.Application;
global using Binding= System.Windows.Data.Binding;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Color = System.Windows.Media.Color;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using ComboBox = System.Windows.Controls.ComboBox;
global using FlowDirection = System.Windows.FlowDirection;
global using KeyEventArgs = System.Windows.Input.KeyEventArgs;

global using MessageBox = System.Windows.MessageBox;
global using Orientation = System.Windows.Controls.Orientation;
global using Screen = Caliburn.Micro.Screen;
global using UserControl = System.Windows.Controls.UserControl;
global using Window = System.Windows.Window;
global using Button = System.Windows.Controls.Button;
