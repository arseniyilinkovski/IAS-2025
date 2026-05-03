using System.Windows;

namespace SimpleIDE.Views
{
    public partial class InputDialog : Window
    {
        public string Answer { get; private set; } = "";

        public InputDialog(string prompt, bool isPassword = false)
        {
            InitializeComponent();
            PromptText.Text = prompt;

            if (isPassword)
            {
                TextInput.Visibility = Visibility.Collapsed;
                PasswordInput.Visibility = Visibility.Visible;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Answer = PasswordInput.Visibility == Visibility.Visible
                ? PasswordInput.Password
                : TextInput.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}