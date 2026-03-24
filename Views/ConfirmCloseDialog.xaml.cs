using System.Windows;

namespace DBF.Views
{
    public enum ConfirmCloseChoice
    {
        ContinueClose, // user chose to continue/close (Ja)
        CancelClose,   // user chose not to close (Nej)
        SaveTime       // user chose "Gem Tiden"
    }

    public partial class ConfirmCloseDialog : Window
    {
        public ConfirmCloseChoice Choice { get; private set; } = ConfirmCloseChoice.CancelClose;

        public ConfirmCloseDialog(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        private void BtnYes_Click(object sender, RoutedEventArgs e)
        {
            Choice = ConfirmCloseChoice.ContinueClose;
            DialogResult = true;
            Close();
        }

        private void BtnNo_Click(object sender, RoutedEventArgs e)
        {
            Choice = ConfirmCloseChoice.CancelClose;
            DialogResult = false;
            Close();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Choice = ConfirmCloseChoice.SaveTime;
            DialogResult = true;
            Close();
        }
    }
}
