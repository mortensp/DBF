using System.Windows;

namespace DBF.Views;

public enum ConfirmCloseChoice
{
      Close     // user chose to continue/close (Ja)
    , Cancel    // user chose not to close (Nej)
    , SaveState // user chose to save the timer state(s)
}

public partial class ConfirmCloseDialog : Window
{
    public ConfirmCloseChoice Choice { get; private set; } = ConfirmCloseChoice.Cancel;

    public ConfirmCloseDialog(string message)
    {
        InitializeComponent();
        MessageText.Text = message;
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        Choice       = ConfirmCloseChoice.Close;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        Choice       = ConfirmCloseChoice.Cancel;
        DialogResult = false;
        Close();
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        Choice       = ConfirmCloseChoice.SaveState;
        DialogResult = true;
        Close();
    }
}
