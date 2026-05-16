using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SimpleIDE.Views
{
    public partial class LoginWindow : Window
    {
        private bool _isLoginMode = true;

        public LoginWindow()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isLoginMode)
            {
                var password = PasswordBox.Password;
                var strength = EvaluatePasswordStrength(password);

                PasswordStrengthBorder.Visibility = Visibility.Visible;
                PasswordStrengthBar.Value = strength.Score;
                PasswordStrengthBar.Background = strength.Color;
                PasswordStrengthText.Text = $"Сложность пароля: {strength.Text}";

                // Проверяем требования
                var hasDigit = password.Any(char.IsDigit);
                var hasUpper = password.Any(char.IsUpper);
                var hasLower = password.Any(char.IsLower);
                var hasMinLength = password.Length >= 6;

                var requirements = new List<string>();

                // Длина
                if (hasMinLength) requirements.Add("✅ 6+ символов");
                else requirements.Add("❌ 6+ символов");

                // Цифры
                if (hasDigit) requirements.Add("✅ цифры");
                else requirements.Add("❌ цифры");

                // Заглавные буквы
                if (hasUpper) requirements.Add("✅ заглавные");
                else requirements.Add("❌ заглавные");

                // Строчные буквы
                if (hasLower) requirements.Add("✅ строчные");
                else requirements.Add("❌ строчные");

                PasswordRequirementsText.Text = string.Join(" • ", requirements);
            }
        }

        private (int Score, string Text, Brush Color) EvaluatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (0, "Слабый", new SolidColorBrush(Color.FromRgb(244, 67, 54)));

            int score = 0;
            int maxScore = 100;

            // Длина
            if (password.Length >= 6) score += 20;
            if (password.Length >= 8) score += 15;
            if (password.Length >= 12) score += 15;

            // Разнообразие символов
            if (password.Any(char.IsDigit)) score += 20;
            if (password.Any(char.IsUpper)) score += 15;
            if (password.Any(char.IsLower)) score += 15;

            score = Math.Min(maxScore, score);

            if (score < 40)
                return (score, "Слабый", new SolidColorBrush(Color.FromRgb(244, 67, 54)));
            else if (score < 70)
                return (score, "Средний", new SolidColorBrush(Color.FromRgb(255, 152, 0)));
            else
                return (score, "Сильный", new SolidColorBrush(Color.FromRgb(76, 175, 80)));
        }

        private async void ActionButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageText.Text = "Введите имя пользователя";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageText.Text = "Введите пароль";
                return;
            }

            if (_isLoginMode)
            {
                // Режим входа
                var success = await App.AuthService.LoginAsync(username, password);
                if (!success)
                {
                    MessageText.Text = "Неверное имя пользователя или пароль";
                    return;
                }
            }
            else
            {
                // Режим регистрации
                var result = await App.AuthService.RegisterAsync(username, password);
                if (!result.Success)
                {
                    MessageText.Text = result.Message;
                    return;
                }

                // Автоматический вход после регистрации
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

            // Очищаем поля
            PasswordBox.Password = "";
            MessageText.Text = "";

            // Скрываем индикатор сложности пароля в режиме входа
            if (_isLoginMode)
            {
                TitleText.Text = "Вход";
                ActionButton.Content = "Войти";
                ToggleText.Text = "Нет аккаунта? Зарегистрироваться";
                PasswordStrengthBorder.Visibility = Visibility.Collapsed;
            }
            else
            {
                TitleText.Text = "Регистрация";
                ActionButton.Content = "Зарегистрироваться";
                ToggleText.Text = "Уже есть аккаунт? Войти";
                // Индикатор сложности показывается только при вводе пароля
                PasswordStrengthBorder.Visibility = Visibility.Collapsed;
            }
        }
    }
}