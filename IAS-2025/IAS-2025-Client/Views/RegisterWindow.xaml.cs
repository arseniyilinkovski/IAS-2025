using IAS_2025_Client.Data;
using IAS_2025_Client.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;

namespace IAS_2025_Client.Views
{
    public partial class RegisterWindow : Window
    {
        private readonly IServiceProvider _services;

        public RegisterWindow(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
            RegisterButton.Click += async (s, e) => await Register();
        }

        private async System.Threading.Tasks.Task Register()
        {
            var username = UsernameBox.Text;
            var email = EmailBox.Text;
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            RegisterButton.IsEnabled = false;

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var authService = new AuthService(context);

            var result = await authService.Register(username, email, password);

            if (result.success)
            {
                MessageBox.Show("Registration successful! Please login.", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
            }
            else
            {
                MessageBox.Show(result.message, "Registration Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                LoadingOverlay.Visibility = Visibility.Collapsed;
                RegisterButton.IsEnabled = true;
            }
        }
    }
}