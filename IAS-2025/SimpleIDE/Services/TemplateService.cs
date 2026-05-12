using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class TemplateService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public TemplateService(ApplicationDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<List<Template>> GetUserTemplatesAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_authService.CurrentUser == null)
                    return new List<Template>();

                return await _context.Templates
                    .Where(t => t.UserId == _authService.CurrentUser.Id || t.IsSystem)
                    .OrderByDescending(t => t.IsSystem)
                    .ThenBy(t => t.Name)
                    .ToListAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Template?> AddTemplateAsync(string name, string content, string? description = null)
        {
            await _semaphore.WaitAsync();
            try
            {
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

                _context.Templates.Add(template);
                await _context.SaveChangesAsync();
                return template;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> DeleteTemplateAsync(int templateId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var template = await _context.Templates.FindAsync(templateId);
                if (template == null || template.IsSystem)
                    return false;

                if (template.UserId != _authService.CurrentUser?.Id)
                    return false;

                _context.Templates.Remove(template);
                await _context.SaveChangesAsync();
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<bool> UpdateTemplateAsync(int templateId, string name, string content, string? description = null)
        {
            await _semaphore.WaitAsync();
            try
            {
                var template = await _context.Templates.FindAsync(templateId);
                if (template == null || template.IsSystem)
                    return false;

                if (template.UserId != _authService.CurrentUser?.Id)
                    return false;

                template.Name = name;
                template.Content = content;
                template.Description = description;
                await _context.SaveChangesAsync();
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}