using SimpleIDE.Models;
using SimpleIDE.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace SimpleIDE.Views
{
    public partial class MainWindow : Window
    {
        private Dictionary<TabItem, FileItem> _openFiles = new Dictionary<TabItem, FileItem>();
        private FileItem? _currentFile;
        private System.Threading.Timer _autoSaveTimer;
        private bool _hasUnsavedChanges = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            UpdateAdminUI();
            _autoSaveTimer = new System.Threading.Timer(AutoSaveCallback, null, 2000, 2000);
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;
        }
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl + S
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                SaveCurrentFile();
                e.Handled = true;
            }
        }
        private async void SaveCurrentFile()
        {
            if (_currentFile != null && CodeEditor != null)
            {
                _currentFile.Content = CodeEditor.Text;
                await App.FileSystemService.UpdateFileContentAsync(_currentFile.Id, CodeEditor.Text);

                var content = CodeEditor.Text;
                var lines = content.Split('\n').Length;
                var chars = content.Length;
                FileInfoText.Text = $"📄 {_currentFile.Name} | Строк: {lines} | Символов: {chars} 💾 сохранено";

                // Сбрасываем сообщение через 2 секунды
                await Task.Delay(2000);
                if (_currentFile != null)
                {
                    FileInfoText.Text = $"📄 {_currentFile.Name} | Строк: {lines} | Символов: {chars}";
                }
            }
        }
        private void AutoSaveCallback(object state)
        {
            if (_hasUnsavedChanges && _currentFile != null)
            {
                Dispatcher.Invoke(async () =>
                {
                    await App.FileSystemService.UpdateFileContentAsync(_currentFile.Id, CodeEditor.Text);
                    _hasUnsavedChanges = false;
                    FileInfoText.Text = FileInfoText.Text.Replace("✨", "💾 сохранено");
                });
            }
        }

        public void UpdateAdminUI()
        {
            if (App.AuthService.IsAdmin)
            {
                AdminButton.Visibility = Visibility.Collapsed;
                UserManagementButton.Visibility = Visibility.Visible;
            }
            else
            {
                AdminButton.Visibility = Visibility.Visible;
                UserManagementButton.Visibility = Visibility.Collapsed;
            }
            UpdateUserInfo();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await App.AuthService.RefreshCurrentUserAsync();

            await LoadFileTree();
            await CheckBackendConnection();

            UpdateAdminUI();    
        }

        private async Task CheckBackendConnection()
        {
            var isConnected = await App.BackendService.CheckHealth();
            if (!isConnected)
            {
                OutputBox.Text = "⚠️ ВНИМАНИЕ: Бекенд не доступен!\n\n" +
                                "Для работы компилятора необходимо запустить бекенд:\n" +
                                "1. Откройте командную строку\n" +
                                "2. Перейдите в папку:\n" +
                                "   cd D:/BGTU/IAS-2025/IAS-2025/Debug/OnlineCompiler/backend\n" +
                                "3. Запустите:\n" +
                                "   python main.py\n" +
                                "4. Должно появиться: Uvicorn running on http://localhost:8000\n\n" +
                                "После запуска бекенда нажмите кнопку '🔄 Обновить'";
            }
        }

        private async Task LoadFileTree()
        {
            try
            {
                var folders = await App.FileSystemService.GetRootFoldersAsync();
                FileTreeView.Items.Clear();

                foreach (var folder in folders)
                {
                    var treeItem = CreateTreeItem(folder);
                    FileTreeView.Items.Add(treeItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки файлов: {ex.Message}");
            }
        }

        private TreeViewItem CreateTreeItem(Folder folder)
        {
            var item = new TreeViewItem
            {
                Header = $"📁 {folder.Name}",
                Tag = folder,
                Foreground = System.Windows.Media.Brushes.White,
                IsExpanded = true
            };

            if (folder.SubFolders != null)
            {
                foreach (var subFolder in folder.SubFolders)
                {
                    item.Items.Add(CreateTreeItem(subFolder));
                }
            }

            if (folder.Files != null)
            {
                foreach (var file in folder.Files)
                {
                    var fileItem = new TreeViewItem
                    {
                        Header = $"📄 {file.Name}",
                        Tag = file,
                        Foreground = System.Windows.Media.Brushes.White
                    };
                    fileItem.MouseDoubleClick += (s, ev) => OpenFile(file);

                    fileItem.ContextMenu = new ContextMenu();
                    var deleteFileMenuItem = new MenuItem { Header = "🗑 Удалить файл" };
                    deleteFileMenuItem.Click += async (s, ev) => await DeleteFile(file);
                    fileItem.ContextMenu.Items.Add(deleteFileMenuItem);

                    item.Items.Add(fileItem);
                }
            }

            item.ContextMenu = new ContextMenu();

            var createFileMenuItem = new MenuItem { Header = "📄 Создать файл" };
            createFileMenuItem.Click += async (s, e) => await CreateFileInFolder(folder);
            item.ContextMenu.Items.Add(createFileMenuItem);

            var createFolderMenuItem = new MenuItem { Header = "📁 Создать папку" };
            createFolderMenuItem.Click += async (s, e) => await CreateFolderInFolder(folder);
            item.ContextMenu.Items.Add(createFolderMenuItem);

            var deleteMenuItem = new MenuItem { Header = "🗑 Удалить папку" };
            deleteMenuItem.Click += async (s, e) => await DeleteFolder(folder);
            item.ContextMenu.Items.Add(deleteMenuItem);

            return item;
        }

        private void OpenFile(FileItem file)
        {
            // Проверяем, не открыт ли уже этот файл
            var existingTab = _openFiles.FirstOrDefault(x => x.Value.Id == file.Id).Key;
            if (existingTab != null)
            {
                FileTabs.SelectedItem = existingTab;
                return;
            }

            // Создаем новую вкладку с заголовком
            var tabItem = new TabItem
            {
                Header = file.Name,
                Tag = file
            };

            FileTabs.Items.Add(tabItem);
            _openFiles[tabItem] = file;
            FileTabs.SelectedItem = tabItem;
            LoadFileToEditor(file);
        }


        private async void FileTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Сохраняем текущий файл перед переключением
            if (_currentFile != null && CodeEditor != null)
            {
                _currentFile.Content = CodeEditor.Text;
                await App.FileSystemService.UpdateFileContentAsync(_currentFile.Id, CodeEditor.Text);
            }

            if (FileTabs.SelectedItem is TabItem selectedTab && _openFiles.ContainsKey(selectedTab))
            {
                var file = _openFiles[selectedTab];
                LoadFileToEditor(file);
            }
        }

        private void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            if (_currentFile != null && CodeEditor != null)
            {
                var content = CodeEditor.Text;
                var lines = content.Split('\n').Length;
                var chars = content.Length;
                FileInfoText.Text = $"📄 {_currentFile.Name} | Строк: {lines} | Символов: {chars} ✨ (не сохранено)";

                _currentFile.Content = content;
                _hasUnsavedChanges = true;
            }
        }

        private async void RunCode_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CodeEditor.Text))
            {
                OutputBox.Text = "❌ Нет кода для выполнения\n\nПожалуйста, введите код в редакторе выше.";
                return;
            }

            var code = CodeEditor.Text;
            OutputBox.Text = "🔄 Компиляция и выполнение...\n\n";

            var result = await App.BackendService.CompileAndRunAsync(code);
            OutputBox.Text = result;
        }
        private async void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tabItem = FindParentTabItem(button);

            if (tabItem != null && _openFiles.ContainsKey(tabItem))
            {
                var file = _openFiles[tabItem];

                // Проверяем наличие несохраненных изменений
                if (_hasUnsavedChanges && _currentFile?.Id == file.Id)
                {
                    var result = MessageBox.Show($"Сохранить изменения в файле '{file.Name}'?",
                        "Несохраненные изменения",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        await App.FileSystemService.UpdateFileContentAsync(file.Id, CodeEditor.Text);
                        file.Content = CodeEditor.Text;
                        _hasUnsavedChanges = false;
                    }
                    else if (result == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                }

                // Закрываем вкладку
                FileTabs.Items.Remove(tabItem);
                _openFiles.Remove(tabItem);

                if (_currentFile?.Id == file.Id)
                {
                    if (_openFiles.Any())
                    {
                        var firstTab = _openFiles.First();
                        FileTabs.SelectedItem = firstTab.Key;
                        LoadFileToEditor(firstTab.Value);
                    }
                    else
                    {
                        _currentFile = null;
                        CodeEditor.Text = "";
                        FileInfoText.Text = "Нет открытого файла";
                        _hasUnsavedChanges = false;
                    }
                }
            }
        }

        private TabItem FindParentTabItem(DependencyObject child)
        {
            while (child != null)
            {
                if (child is TabItem tabItem)
                    return tabItem;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
        private void LoadFileToEditor(FileItem file)
        {
            _currentFile = file;
            var content = file.Content ?? "";
            content = content.Replace("\r\n", "\n");
            CodeEditor.Text = content;
            _hasUnsavedChanges = false;
            UpdateFileInfo(file);
            CodeEditor.FocusEditor();
        }
        private void UpdateFileInfo(FileItem file)
        {
            var content = file.Content ?? "";
            var lines = content.Split('\n').Length;
            var chars = content.Length;
            FileInfoText.Text = $"📄 {file.Name} | Строк: {lines} | Символов: {chars}";
        }


        private async void CreateRootFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите имя файла (например: main.ias)");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                var newFile = await App.FileSystemService.CreateFileAsync(dialog.Answer, "");
                await LoadFileTree();
                OpenFile(newFile);
            }
        }

        private async void CreateRootFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new InputDialog("Введите имя папки");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                await App.FileSystemService.CreateFolderAsync(dialog.Answer);
                await LoadFileTree();
            }
        }

        private async Task CreateFileInFolder(Folder folder)
        {
            var dialog = new InputDialog($"Введите имя файла в папке '{folder.Name}'");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                var newFile = await App.FileSystemService.CreateFileAsync(dialog.Answer, "", folder.Id);
                await LoadFileTree();
                OpenFile(newFile);
            }
        }

        private async Task CreateFolderInFolder(Folder folder)
        {
            var dialog = new InputDialog($"Введите имя папки внутри '{folder.Name}'");
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.Answer))
            {
                await App.FileSystemService.CreateFolderAsync(dialog.Answer, folder.Id);
                await LoadFileTree();
            }
        }
        public async void SaveAllOpenFiles()
        {
            foreach (var file in _openFiles.Values)
            {
                if (file.Content != null)
                {
                    await App.FileSystemService.UpdateFileContentAsync(file.Id, file.Content);
                }
            }
        }
        private async Task DeleteFile(FileItem file)
        {
            var result = MessageBox.Show($"Удалить файл '{file.Name}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await App.FileSystemService.DeleteFileAsync(file.Id);

                var tabToClose = _openFiles.FirstOrDefault(x => x.Value.Id == file.Id).Key;
                if (tabToClose != null)
                {
                    FileTabs.Items.Remove(tabToClose);
                    _openFiles.Remove(tabToClose);
                }

                if (_currentFile?.Id == file.Id)
                {
                    _currentFile = null;
                    CodeEditor.Text = "";
                    FileInfoText.Text = "Нет открытого файла";
                }

                await LoadFileTree();
            }
        }

        private async Task DeleteFolder(Folder folder)
        {
            var result = MessageBox.Show($"Удалить папку '{folder.Name}' и все её содержимое?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await App.FileSystemService.DeleteFolderAsync(folder.Id);
                await LoadFileTree();
            }
        }

        private void LoadExample_Click(object sender, RoutedEventArgs e)
        {
            var exampleCode = @"int function get_sum(int param argOne, int param argTwo){
	int var res = argOne + argTwo;
	return res;
}

bool function IsStringsEquals(str param StringOne, str param StringTwo){
	bool var res;
	int var lenOne = lenght(StringOne);
	int var lenTwo = lenght(StringTwo);
	if (lenOne & lenTwo)
	then{
		res = true;
	}
	else{
		res = false;
	}
	return res;
}	
bool function IsEven(int param arg){
	bool var res;
	int var buf = arg % 2;
	if (buf & 0)
	then{
		res = true;
	}
	else{
		res = false;
	}
	return res;
}
main
{
output ""n"";
str var date = getLocalTimeAndDate();
output date;
int var a = b1010; #10
output ""Переменная a"";
output a;
int var b = h32; #50
output ""Переменная b"";
output b;
int var rnd = random(a, b);
output ""Случайное число от a до b"";
output rnd;
char var ch = 'c';
int var code = asciiCode(ch);
output ""Код ch"";
output code;
a = a >> 2; 	
output ""a после сдвига вправо на 2"";
output a;
b = b << 2;
output ""b после сдвига влево на 2"";
output b;
int var iter = 0;
repeat(10){
	output iter;
	iter = iter + 1;
}
repeat(iter > 1){
	output ""new iter"";
	output iter;
	iter = iter - 1;
}

output ""Выполнение функции get_sum(a, b)"";
int var sum = get_sum(a, b);
output sum;

str var stringOne = ""Hello, my name is Arseniy"";
str var stringTwo = ""Hello, my name is Alex"";

output ""Первые три элемента, скопированные из первой строки во вторую :"";
str var ns = copy(stringOne, stringTwo, 3 );
output ns;
output stringOne;
output stringTwo;

bool var flag = IsStringsEquals(stringOne,stringTwo);
output flag;
int var test = 2;
output ""test:"";
output test;
test = powNumber(test, 3);
output ""test после powNumber:"";
output test;
test = factorialOfNumber(test);
output ""Факториал переменной test:"";
output test;
bool var evenFlag = IsEven(test);
output evenFlag;
int var oct = o12; #10
output oct;
}";

            CodeEditor.Text = exampleCode;
            OutputBox.Text = "📋 Пример кода загружен!\nНажмите ▶ Запустить для выполнения.";
        }

        private void CopyOutput_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(OutputBox.Text))
            {
                Clipboard.SetText(OutputBox.Text);
                MessageBox.Show("Вывод скопирован в буфер обмена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearOutput_Click(object sender, RoutedEventArgs e)
        {
            OutputBox.Text = "";
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadFileTree();
            await CheckBackendConnection();
        }
        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.ContextMenu != null)
            {
                button.ContextMenu.IsOpen = true;
            }
        }
        private void ThemePickMe_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(AppTheme.PickMe);
            RefreshAllStyles();
            UpdateButtonTexts(); // Явно вызываем обновление текстов
        }

        private void ThemeDark_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(AppTheme.Dark);
            RefreshAllStyles();
            UpdateButtonTexts();
        }

        private void ThemeDracula_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(AppTheme.Dracula);
            RefreshAllStyles();
            UpdateButtonTexts();
        }

        private void ThemeNord_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(AppTheme.Nord);
            RefreshAllStyles();
            UpdateButtonTexts();
        }

        private void UpdateButtonTexts()
        {
            if (ThemeService.CurrentTheme == AppTheme.PickMe)
            {
                RunButton.Content = ThemeService.GetLocalizedString("RunButtonText", "▶ Запустить");
                ExampleButton.Content = ThemeService.GetLocalizedString("ExampleButtonText", "📋 Пример");
                AdminButton.Content = ThemeService.GetLocalizedString("AdminButtonText", "👑 Стать администратором");
                UserManagementButton.Content = ThemeService.GetLocalizedString("UsersButtonText", "👥 Пользователи");
                RefreshButton.Content = ThemeService.GetLocalizedString("RefreshButtonText", "🔄 Обновить");
                LogoutButton.Content = ThemeService.GetLocalizedString("LogoutButtonText", "🚪 Выйти");

                // Обновляем заголовки
                LeftPanelTitle.Text = ThemeService.GetLocalizedString("ProjectTitle", "Мои проекты");
                RightClickHint.Text = ThemeService.GetLocalizedString("RightClickHint", "Правой кнопкой мыши для создания");
                OutputTitle.Text = ThemeService.GetLocalizedString("OutputTitle", "ВЫВОД ПРОГРАММЫ");

                if (_currentFile == null)
                {
                    FileInfoText.Text = ThemeService.GetLocalizedString("NoFileText", "Нет открытого файла");
                }
            }
            else
            {
                // Восстанавливаем стандартные тексты
                RunButton.Content = "▶ Запустить";
                ExampleButton.Content = "📋 Пример";
                AdminButton.Content = "👑 Стать администратором";
                UserManagementButton.Content = "👥 Пользователи";
                RefreshButton.Content = "🔄 Обновить";
                LogoutButton.Content = "🚪 Выйти";

                LeftPanelTitle.Text = "Мои проекты";
                RightClickHint.Text = "Правой кнопкой мыши для создания";
                OutputTitle.Text = "📋 ВЫВОД ПРОГРАММЫ";

                if (_currentFile == null)
                {
                    FileInfoText.Text = "Нет открытого файла";
                }
            }
        }

        private void RefreshAllStyles()
        {
            // Обновляем фон главного окна
            Background = (SolidColorBrush)App.Current.Resources["BackgroundDark"];

            // Обновляем дерево файлов
            FileTreeView.Background = (SolidColorBrush)App.Current.Resources["BackgroundLight"];
            FileTreeView.Foreground = (SolidColorBrush)App.Current.Resources["TextPrimary"];

            // Обновляем дерево вкладок
            FileTabs.Background = (SolidColorBrush)App.Current.Resources["BackgroundLight"];

            // Обновляем Output
            OutputBox.Background = (SolidColorBrush)App.Current.Resources["BackgroundDark"];
            OutputBox.Foreground = (SolidColorBrush)App.Current.Resources["TextPrimary"];

            // Обновляем тексты кнопок
            UpdateButtonTexts();

            // Обновляем текст приветствия в OutputBox
            if (ThemeService.CurrentTheme == AppTheme.PickMe)
            {
                OutputBox.Text = ThemeService.GetLocalizedString("WelcomeText", "✨ Добро пожаловать в SimpleIDE! ✨");
            }
            else
            {
                OutputBox.Text = "✨ Добро пожаловать в SimpleIDE! ✨";
            }
        }



        // Вспомогательный метод для поиска дочерних элементов
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t)
                    yield return t;
                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
        private async void BecomeAdmin_Click(object sender, RoutedEventArgs e)
        {
            var passwordDialog = new InputDialog("Введите пароль администратора", true);
            if (passwordDialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(passwordDialog.Answer))
            {
                var currentUser = App.AuthService.CurrentUser;
                if (currentUser != null)
                {
                    var success = await App.AuthService.MakeAdminAsync(passwordDialog.Answer, currentUser.Id);
                    if (success)
                    {
                        MessageBox.Show("Вы стали администратором!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        UpdateAdminUI();
                    }
                    else
                    {
                        MessageBox.Show("Неверный пароль администратора!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        
        // Обновление информации о пользователе 
        private void UpdateUserInfo()
        {
            var currentUser = App.AuthService.CurrentUser;
            if (currentUser != null)
            {
                UserNameText.Text = currentUser.Username;

                if (App.AuthService.IsAdmin)
                {
                    AvatarText.Text = "👑";
                    UserRoleText.Text = "Администратор";
                    UserRoleText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(156, 39, 176)); // Фиолетовый
                }
                else
                {
                    AvatarText.Text = GetAvatarLetter(currentUser.Username);
                    UserRoleText.Text = "Пользователь";
                    UserRoleText.Foreground = new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(76, 175, 80)); // Зеленый
                }
            }
        }
        private string GetAvatarLetter(string username)
        {
            if (string.IsNullOrEmpty(username)) return "?";
            return username.Substring(0, 1).ToUpper();
        }

        private async void UserManagement_Click(object sender, RoutedEventArgs e)
        {
            // Упрощенная проверка - просто проверяем свойство IsAdmin
            if (!App.AuthService.IsAdmin)
            {
                MessageBox.Show("У вас нет прав администратора!",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var userWindow = new UserManagementWindow();
            userWindow.Owner = this;
            userWindow.ShowDialog();

            // После закрытия окна управления обновляем статус
            await App.AuthService.RefreshCurrentUserAsync();
            UpdateAdminUI();
        }


        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            ThemeService.ApplyTheme(AppTheme.Dark);

            App.AuthService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}