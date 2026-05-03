using System.Windows;
using System.Windows.Input;

namespace SimpleIDE.Views
{
    public partial class LoginWindow : Window
    {
        private bool _isLoginMode = true;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageText.Text = "Заполните все поля";
                return;
            }

            bool success;

            if (_isLoginMode)
            {
                success = await App.AuthService.LoginAsync(username, password);
                if (!success)
                {
                    MessageText.Text = "Неверное имя пользователя или пароль";
                    return;
                }
            }
            else
            {
                success = await App.AuthService.RegisterAsync(username, password);
                if (!success)
                {
                    MessageText.Text = "Пользователь с таким именем уже существует";
                    return;
                }

                await App.AuthService.LoginAsync(username, password);
            }

            // Открываем главное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();
            Close();
        }

        private void ToggleMode_Click(object sender, MouseButtonEventArgs e)
        {
            _isLoginMode = !_isLoginMode;

            if (_isLoginMode)
            {
                TitleText.Text = "Вход";
                ActionButton.Content = "Войти";
                ToggleText.Text = "Нет аккаунта? Зарегистрироваться";
                MessageText.Text = "";
            }
            else
            {
                TitleText.Text = "Регистрация";
                ActionButton.Content = "Зарегистрироваться";
                ToggleText.Text = "Уже есть аккаунт? Войти";
                MessageText.Text = "";
            }
        }
    }
}