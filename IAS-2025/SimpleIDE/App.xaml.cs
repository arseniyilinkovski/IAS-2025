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
        public static ApplicationDbContext DbContext { get; private set; } = null!;
        public static AuthService AuthService { get; private set; } = null!;
        public static FileSystemService FileSystemService { get; private set; } = null!;
        public static BackendService BackendService { get; private set; } = null!;
        public static TemplateService TemplateService { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            DbContext = new ApplicationDbContext();
            DbContext.Database.EnsureCreated();

            AuthService = new AuthService(DbContext);
            FileSystemService = new FileSystemService(DbContext, AuthService);
            TemplateService = new TemplateService(DbContext, AuthService); // <-- ДОБАВЬ ЭТУ СТРОКУ

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