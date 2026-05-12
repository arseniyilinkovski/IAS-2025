using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;
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
            TemplateService = new TemplateService(AuthService);
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
    public class TemplateService
    {
        private readonly AuthService _authService;

        public TemplateService(AuthService authService)
        {
            _authService = authService;
        }

        public async Task<List<Template>> GetUserTemplatesAsync()
        {
            if (_authService.CurrentUser == null)
                return new List<Template>();

            using (var context = new ApplicationDbContext())
            {
                return await context.Templates
                    .Where(t => t.UserId == _authService.CurrentUser.Id || t.IsSystem)
                    .OrderByDescending(t => t.IsSystem)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
        }

        public async Task<Template?> AddTemplateAsync(string name, string content, string? description = null)
        {
            if (_authService.CurrentUser == null)
                return null;

            using (var context = new ApplicationDbContext())
            {
                var template = new Template
                {
                    Name = name,
                    Content = content,
                    Description = description,
                    UserId = _authService.CurrentUser.Id,
                    IsSystem = false,
                    CreatedAt = DateTime.Now
                };

                context.Templates.Add(template);
                await context.SaveChangesAsync();
                return template;
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            using (var context = new ApplicationDbContext())
            {
                var template = await context.Templates.FindAsync(templateId);
                if (template == null || template.IsSystem)
                    return false;

                if (template.UserId != _authService.CurrentUser?.Id)
                    return false;

                context.Templates.Remove(template);
                await context.SaveChangesAsync();
                return true;
            }
        }
    }
}