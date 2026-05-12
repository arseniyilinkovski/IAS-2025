using System.Windows;
using Microsoft.EntityFrameworkCore;

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
            // Проверяем права администратора в реальном времени из БД
            var isAdmin = await App.AuthService.IsAdminRealTimeAsync();

            if (!isAdmin)
            {
                MessageBox.Show("У вас нет прав администратора! Ваши права были отозваны.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
                return;
            }

            await LoadUsers();
            UsersGrid.CellEditEnding += UsersGrid_CellEditEnding;
        }

        private async Task LoadUsers()
        {
            var users = await App.AuthService.GetAllUsersAsync();
            UsersGrid.ItemsSource = users;
        }

        private async void UsersGrid_CellEditEnding(object sender, System.Windows.Controls.DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.DisplayIndex == 3) // Колонка Администратор
            {
                // Проверяем права перед сохранением
                var isAdmin = await App.AuthService.IsAdminRealTimeAsync();
                if (!isAdmin)
                {
                    MessageBox.Show("У вас нет прав администратора! Изменения не сохранены.",
                        "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                    await LoadUsers();
                    Close();
                    return;
                }

                var user = (Models.User)e.Row.Item;
                var checkBox = e.EditingElement as System.Windows.Controls.CheckBox;

                if (checkBox != null)
                {
                    bool newValue = checkBox.IsChecked ?? false;

                    var success = await App.AuthService.SetAdminRoleAsync(user.Id, newValue);

                    if (success)
                    {
                        MessageBox.Show($"Статус пользователя '{user.Username}' изменен!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        // Обновляем текущего пользователя
                        await App.AuthService.RefreshCurrentUserAsync();

                        // Обновляем UI главного окна
                        var mainWindow = Application.Current.Windows
                            .OfType<MainWindow>()
                            .FirstOrDefault();
                        mainWindow?.UpdateAdminUI();

                        await LoadUsers();
                    }
                    else
                    {
                        MessageBox.Show("Ошибка при изменении статуса!", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        await LoadUsers();
                    }
                }
            }
        }
    }
}