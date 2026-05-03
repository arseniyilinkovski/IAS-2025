using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using IAS_2025_Client.Data;
using IAS_2025_Client.Models;
using IAS_2025_Client.Services;

namespace IAS_2025_Client.ViewModels
{
    public class FileTreeNode : BaseViewModel
    {
        private object? _item;
        private string _name = string.Empty;
        private string _type = string.Empty;

        public object? Item
        {
            get => _item;
            set => SetField(ref _item, value);
        }

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Type
        {
            get => _type;
            set => SetField(ref _type, value);
        }

        public ObservableCollection<FileTreeNode> Children { get; set; } = new();
        public bool IsExpanded { get; set; }
        public bool IsSelected { get; set; }
    }

    public class MainViewModel : BaseViewModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ApiService _apiService;
        private readonly AuthService _authService;

        private string _currentCode = "// Write your IAS-2025 code here\n\nprogram Main;\nbegin\n    // Your code\nend.";
        private string _output = "";
        private bool _isLoading;
        private FileTreeNode? _selectedNode;
        private ObservableCollection<User> _users = new();
        private bool _showUsersPanel;

        public string CurrentCode
        {
            get => _currentCode;
            set => SetField(ref _currentCode, value);
        }

        public string Output
        {
            get => _output;
            set => SetField(ref _output, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public FileTreeNode? SelectedNode
        {
            get => _selectedNode;
            set
            {
                SetField(ref _selectedNode, value);
                if (value?.Item is FileItem file)
                {
                    CurrentCode = file.Content;
                }
            }
        }

        public ObservableCollection<User> Users
        {
            get => _users;
            set => SetField(ref _users, value);
        }

        public bool ShowUsersPanel
        {
            get => _showUsersPanel && _authService.CurrentUser?.IsAdmin == true;
            set => SetField(ref _showUsersPanel, value);
        }

        public ObservableCollection<FileTreeNode> FileTree { get; set; } = new();
        public ICommand RunCodeCommand { get; }
        public ICommand NewFileCommand { get; }
        public ICommand NewFolderCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand BecomeAdminCommand { get; }
        public ICommand MakeUserAdminCommand { get; }
        public ICommand RefreshUsersCommand { get; }

        public MainViewModel(ApplicationDbContext context, ApiService apiService, AuthService authService)
        {
            _context = context;
            _apiService = apiService;
            _authService = authService;

            RunCodeCommand = new RelayCommand(async _ => await RunCode(), _ => !IsLoading);
            NewFileCommand = new RelayCommand(async _ => await CreateNewFile());
            NewFolderCommand = new RelayCommand(async _ => await CreateNewFolder());
            SaveFileCommand = new RelayCommand(async _ => await SaveCurrentFile());
            DeleteItemCommand = new RelayCommand(async _ => await DeleteSelectedItem());
            BecomeAdminCommand = new RelayCommand(async _ => await ShowAdminPasswordDialog());
            MakeUserAdminCommand = new RelayCommand(async param => await MakeUserAdmin(param));
            RefreshUsersCommand = new RelayCommand(async _ => await LoadUsers());

            LoadFileTree();
            LoadUsers();
        }

        private async Task LoadFileTree()
        {
            if (_authService.CurrentUser == null) return;

            FileTree.Clear();

            var rootFolders = await _context.Folders
                .Where(f => f.UserId == _authService.CurrentUser.Id && f.ParentFolderId == null)
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .ToListAsync();

            foreach (var folder in rootFolders)
            {
                FileTree.Add(BuildTreeNode(folder));
            }
        }

        private FileTreeNode BuildTreeNode(Folder folder)
        {
            var node = new FileTreeNode
            {
                Item = folder,
                Name = folder.Name,
                Type = "folder"
            };

            foreach (var subFolder in folder.SubFolders)
            {
                node.Children.Add(BuildTreeNode(subFolder));
            }

            foreach (var file in folder.Files)
            {
                node.Children.Add(new FileTreeNode
                {
                    Item = file,
                    Name = file.Name,
                    Type = "file"
                });
            }

            return node;
        }

        private async Task CreateNewFile()
        {
            var folder = await GetSelectedFolder();
            if (folder == null)
            {
                MessageBox.Show("Please select a folder first", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var fileName = $"NewFile_{DateTime.Now.Ticks}.ias";
            var file = new FileItem
            {
                Name = fileName,
                Content = "// New IAS-2025 file\n\nprogram Main;\nbegin\n    \nend.",
                FolderId = folder.Id,
                UserId = _authService.CurrentUser!.Id
            };

            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            await LoadFileTree();
        }

        private async Task CreateNewFolder()
        {
            var parentFolder = await GetSelectedFolder();
            var folderName = $"NewFolder_{DateTime.Now.Ticks}";

            var folder = new Folder
            {
                Name = folderName,
                UserId = _authService.CurrentUser!.Id,
                ParentFolderId = parentFolder?.Id,
                Path = parentFolder?.Path + "/" + folderName ?? folderName
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
            await LoadFileTree();
        }

        private async Task<Folder?> GetSelectedFolder()
        {
            if (SelectedNode?.Item is Folder folder)
                return folder;

            if (SelectedNode?.Item is FileItem file)
                return await _context.Folders.FindAsync(file.FolderId);

            return null;
        }

        private async Task SaveCurrentFile()
        {
            if (SelectedNode?.Item is FileItem file)
            {
                file.Content = CurrentCode;
                file.ModifiedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                MessageBox.Show("File saved successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task DeleteSelectedItem()
        {
            if (SelectedNode == null) return;

            var result = MessageBox.Show($"Delete {SelectedNode.Name}?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxQuestion);

            if (result != MessageBoxResult.Yes) return;

            if (SelectedNode.Item is FileItem file)
            {
                _context.Files.Remove(file);
            }
            else if (SelectedNode.Item is Folder folder)
            {
                _context.Folders.Remove(folder);
            }

            await _context.SaveChangesAsync();
            await LoadFileTree();
        }

        private async Task RunCode()
        {
            if (string.IsNullOrWhiteSpace(CurrentCode))
            {
                Output = "Error: No code to execute";
                return;
            }

            IsLoading = true;
            Output = "Executing...";

            var result = await _apiService.ExecuteCode(CurrentCode);

            if (result.success)
            {
                Output = result.output;
            }
            else
            {
                Output = $"Error: {result.error}\n{result.output}";
            }

            IsLoading = false;
        }

        private async Task ShowAdminPasswordDialog()
        {
            var dialog = new PasswordDialog("Enter Admin Password");
            if (dialog.ShowDialog() == true && dialog.Password == "admin123") // Change this to your admin password
            {
                if (_authService.CurrentUser != null)
                {
                    _authService.CurrentUser.IsAdmin = true;
                    await _context.SaveChangesAsync();
                    OnPropertyChanged(nameof(ShowUsersPanel));
                    await LoadUsers();
                    MessageBox.Show("You are now an administrator!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else if (dialog.Password != null)
            {
                MessageBox.Show("Invalid admin password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MakeUserAdmin(object param)
        {
            if (param is User user && _authService.CurrentUser?.IsAdmin == true)
            {
                var passwordDialog = new PasswordDialog("Confirm with your admin password");
                if (passwordDialog.ShowDialog() == true)
                {
                    var success = await _authService.MakeAdmin(user.Id, passwordDialog.Password);
                    if (success)
                    {
                        await LoadUsers();
                        MessageBox.Show($"{user.Username} is now an administrator!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Invalid admin password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async Task LoadUsers()
        {
            if (_authService.CurrentUser?.IsAdmin == true)
            {
                var users = await _authService.GetAllUsers();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);
    }
}