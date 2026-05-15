using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class FileSystemService
    {
        private readonly AuthService _authService;

        public FileSystemService(AuthService authService)
        {
            _authService = authService;
        }

        private ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext();
        }

        public async Task<Folder> CreateFolderAsync(string name, int? parentFolderId = null)
        {
            using var context = CreateContext();

            if (_authService.CurrentUser == null)
                throw new UnauthorizedAccessException();

            var folder = new Folder
            {
                Name = name,
                UserId = _authService.CurrentUser.Id,
                ParentFolderId = parentFolderId,
                CreatedAt = DateTime.Now
            };

            context.Folders.Add(folder);
            await context.SaveChangesAsync();
            return folder;
        }

        public async Task<FileItem> CreateFileAsync(string name, string content, int? folderId = null)
        {
            using var context = CreateContext();

            if (_authService.CurrentUser == null)
                throw new UnauthorizedAccessException();

            var file = new FileItem
            {
                Name = name,
                Content = content,
                FolderId = folderId,
                UserId = _authService.CurrentUser.Id,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Files.Add(file);
            await context.SaveChangesAsync();
            return file;
        }

        public async Task<List<Folder>> GetRootFoldersAsync()
        {
            using var context = CreateContext();

            if (_authService.CurrentUser == null)
                return new List<Folder>();

            return await context.Folders
                .Where(f => f.UserId == _authService.CurrentUser.Id && f.ParentFolderId == null)
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .ToListAsync();
        }

        public async Task UpdateFileContentAsync(int fileId, string content)
        {
            using var context = CreateContext();

            var file = await context.Files.FindAsync(fileId);
            if (file != null && file.UserId == _authService.CurrentUser?.Id)
            {
                file.Content = content;
                file.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteFileAsync(int fileId)
        {
            using var context = CreateContext();

            var file = await context.Files.FindAsync(fileId);
            if (file != null && file.UserId == _authService.CurrentUser?.Id)
            {
                context.Files.Remove(file);
                await context.SaveChangesAsync();
            }
        }

        public async Task DeleteFolderAsync(int folderId)
        {
            using var context = CreateContext();

            var folder = await context.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder != null && folder.UserId == _authService.CurrentUser?.Id)
            {
                context.Folders.Remove(folder);
                await context.SaveChangesAsync();
            }
        }
    }
}