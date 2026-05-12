using System.Windows;
using System.Windows.Controls;
using SimpleIDE.Models;

namespace SimpleIDE.Views
{
    public partial class MainWindow : Window
    {
        private Dictionary<TabItem, FileItem> _openFiles = new Dictionary<TabItem, FileItem>();
        private FileItem? _currentFile;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            UpdateAdminUI();
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
            var existingTab = _openFiles.FirstOrDefault(x => x.Value.Id == file.Id).Key;
            if (existingTab != null)
            {
                FileTabs.SelectedItem = existingTab;
                return;
            }

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

        private void LoadFileToEditor(FileItem file)
        {
            _currentFile = file;
            var content = file.Content ?? "";
            content = content.Replace("\r\n", "\n");
            CodeEditor.Text = content;
            var lines = content.Split('\n').Length;
            var chars = content.Length;
            FileInfoText.Text = $"📄 {file.Name} | Строк: {lines} | Символов: {chars}";
            CodeEditor.FocusEditor();
        }

        private void FileTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileTabs.SelectedItem is TabItem selectedTab && _openFiles.ContainsKey(selectedTab))
            {
                var file = _openFiles[selectedTab];
                LoadFileToEditor(file);
            }
        }

        private async void CodeEditor_TextChanged(object sender, EventArgs e)
        {
            if (_currentFile != null && CodeEditor != null)
            {
                await App.FileSystemService.UpdateFileContentAsync(_currentFile.Id, CodeEditor.Text);
                var content = CodeEditor.Text;
                var lines = content.Split('\n').Length;
                var chars = content.Length;
                FileInfoText.Text = $"📄 {_currentFile.Name} | Строк: {lines} | Символов: {chars} ✨ сохранено";
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

output ""Первых три элемента, скопированных из первой строки во вторую :"";
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
            // Проверяем права администратора в реальном времени
            var isAdmin = await App.AuthService.IsAdminRealTimeAsync();

            if (!isAdmin)
            {
                MessageBox.Show("У вас нет прав администратора! Ваши права были отозваны.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Обновляем статус в UI
                await App.AuthService.RefreshCurrentUserAsync();
                UpdateAdminUI();
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
            App.AuthService.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            Close();
        }
    }
}