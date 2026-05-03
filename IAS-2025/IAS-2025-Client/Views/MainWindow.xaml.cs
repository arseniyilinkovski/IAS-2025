using IAS_2025_Client.Data;
using IAS_2025_Client.Services;
using IAS_2025_Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace IAS_2025_Client.Views
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _services;
        private readonly AuthService _authService;
        private readonly MainViewModel _viewModel;

        public MainWindow(IServiceProvider services, AuthService authService)
        {
            InitializeComponent();
            _services = services;
            _authService = authService;

            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var apiService = new ApiService();

            _viewModel = new MainViewModel(context, apiService, _authService);
            DataContext = _viewModel;

            AdminButton.Click += (s, e) => ToggleAdminPanel();
            LogoutButton.Click += (s, e) => Logout();

            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                _viewModel.RunCodeCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void FileTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is ViewModels.FileTreeNode node)
            {
                _viewModel.SelectedNode = node;
            }
        }

        private void ToggleAdminPanel()
        {
            if (_authService.CurrentUser?.IsAdmin != true)
            {
                var passwordDialog = new PasswordDialog("Enter Admin Password to access admin panel");
                if (passwordDialog.ShowDialog() == true && passwordDialog.Password == "admin123")
                {
                    _authService.CurrentUser.IsAdmin = true;
                }
                else
                {
                    MessageBox.Show("Access denied", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var isVisible = AdminPanel.Visibility == Visibility.Visible;
            AdminPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;

            if (!isVisible && _authService.CurrentUser?.IsAdmin == true)
            {
                _viewModel.ShowUsersPanel = true;
                _ = _viewModel.RefreshUsersCommand.Execute(null);
            }
        }

        private async void Logout()
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Logout",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _authService.Logout();

                var loginWindow = new LoginWindow(_services);
                loginWindow.Show();
                Close();
            }
        }
    }

    public class PasswordDialog : Window
    {
        public string Password { get; private set; } = "";

        public PasswordDialog(string title)
        {
            Title = title;
            Width = 350;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;

            var border = new Border
            {
                Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(26, 26, 46),
                    System.Windows.Media.Color.FromRgb(22, 33, 62),
                    90),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(0, 0, 0, 15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(titleText, 0);
            grid.Children.Add(titleText);

            var passwordBox = new System.Windows.Controls.PasswordBox
            {
                FontSize = 13,
                Height = 35,
                Margin = new Thickness(0, 0, 0, 15)
            };
            Grid.SetRow(passwordBox, 1);
            grid.Children.Add(passwordBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };
            var okButton = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), Cursor = Cursors.Hand };
            var cancelButton = new Button { Content = "Cancel", Width = 80, Height = 30, Cursor = Cursors.Hand };

            okButton.Click += (s, e) => { Password = passwordBox.Password; DialogResult = true; Close(); };
            cancelButton.Click += (s, e) => { DialogResult = false; Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            border.Child = grid;
            Content = border;

            passwordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Password = passwordBox.Password;
                    DialogResult = true;
                    Close();
                }
            };
        }
    }
}