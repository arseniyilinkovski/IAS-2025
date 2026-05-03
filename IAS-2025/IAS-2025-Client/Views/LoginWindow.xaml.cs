using IAS_2025_Client.Data;
using IAS_2025_Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IAS_2025_Client.Views
{
    public partial class LoginWindow : Window
    {
        private readonly IServiceProvider _services;

        public LoginWindow(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            LoginButton.Click += async (s, e) => await Login();
            PasswordBox.KeyDown += async (s, e) =>
            {
                if (e.Key == Key.Enter) await Login();
            };

            RegisterLink.MouseLeftButtonDown += (s, e) => OpenRegisterWindow();
        }

        private async System.Threading.Tasks.Task Login()
        {
            var username = UsernameBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter username and password", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            LoginButton.IsEnabled = false;

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var authService = new AuthService(context);

            var result = await authService.Login(username, password);

            if (result.success)
            {
                var mainWindow = new MainWindow(_services, authService);
                mainWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show(result.message, "Login Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingOverlay.Visibility = Visibility.Collapsed;
                LoginButton.IsEnabled = true;
            }
        }

        private void OpenRegisterWindow()
        {
            var registerWindow = new RegisterWindow(_services);
            registerWindow.ShowDialog();
        }
    }
}