using System.Windows;

namespace SimpleIDE.Views
{
    public partial class UserManagementWindow : Window
    {
        public UserManagementWindow()
        {
            InitializeComponent();
            Loaded += UserManagementWindow_Loaded;
        }

        private async void UserManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!App.AuthService.IsAdmin)
            {
                MessageBox.Show("Доступ запрещен");
                Close();
                return;
            }

            var users = await App.AuthService.GetAllUsersAsync();
            UsersGrid.ItemsSource = users;

            UsersGrid.CellEditEnding += async (s, args) =>
            {
                if (args.Column.DisplayIndex == 3)
                {
                    var user = (Models.User)args.Row.Item;
                    await App.AuthService.SetAdminRoleAsync(user.Id, user.IsAdmin);
                }
            };
        }
    }
}