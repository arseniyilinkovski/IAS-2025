using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Services;
using SimpleIDE.Views;
using System.Net.Http;
using System.Windows;

namespace SimpleIDE
{
    public partial class App : Application
    {
        public static ApplicationDbContext DbContext { get; private set; }
        public static AuthService AuthService { get; private set; }
        public static FileSystemService FileSystemService { get; private set; }
        public static BackendService BackendService { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            DbContext = new ApplicationDbContext();
            DbContext.Database.EnsureCreated();

            AuthService = new AuthService(DbContext);
            FileSystemService = new FileSystemService(DbContext, AuthService);

            // Создаем HttpClient с правильными настройками
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            BackendService = new BackendService(httpClient);

            var loginWindow = new LoginWindow();
            loginWindow.Show();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            DbContext?.Dispose();
            base.OnExit(e);
        }
    }
}