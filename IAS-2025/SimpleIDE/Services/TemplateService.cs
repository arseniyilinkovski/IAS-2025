using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class TemplateService
    {
        private readonly AuthService _authService;

        public TemplateService(AuthService authService)
        {
            _authService = authService;
        }

        private ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext();
        }

        public async Task<List<Template>> GetUserTemplatesAsync()
        {
            using var context = CreateContext();

            if (_authService.CurrentUser == null)
                return new List<Template>();

            return await context.Templates
                .Where(t => t.UserId == _authService.CurrentUser.Id || t.IsSystem)
                .OrderByDescending(t => t.IsSystem)
                .ThenBy(t => t.Name)
                .ToListAsync();
        }

        public async Task<Template?> AddTemplateAsync(string name, string content, string? description = null)
        {
            using var context = CreateContext();

            if (_authService.CurrentUser == null)
                return null;

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

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            using var context = CreateContext();

            var template = await context.Templates.FindAsync(templateId);
            if (template == null || template.IsSystem)
                return false;

            if (template.UserId != _authService.CurrentUser?.Id)
                return false;

            context.Templates.Remove(template);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateTemplateAsync(int templateId, string name, string content, string? description = null)
        {
            using var context = CreateContext();

            var template = await context.Templates.FindAsync(templateId);
            if (template == null || template.IsSystem)
                return false;

            if (template.UserId != _authService.CurrentUser?.Id)
                return false;

            template.Name = name;
            template.Content = content;
            template.Description = description;
            await context.SaveChangesAsync();
            return true;
        }
    }
}