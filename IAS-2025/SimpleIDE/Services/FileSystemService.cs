using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class FileSystemService
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public FileSystemService(ApplicationDbContext context, AuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        public async Task<Folder> CreateFolderAsync(string name, int? parentFolderId = null)
        {
            if (_authService.CurrentUser == null)
                throw new UnauthorizedAccessException();

            var folder = new Folder
            {
                Name = name,
                UserId = _authService.CurrentUser.Id,
                ParentFolderId = parentFolderId
            };

            _context.Folders.Add(folder);
            await _context.SaveChangesAsync();
            return folder;
        }

        public async Task<FileItem> CreateFileAsync(string name, string content, int? folderId = null)
        {
            if (_authService.CurrentUser == null)
                throw new UnauthorizedAccessException();

            var file = new FileItem
            {
                Name = name,
                Content = content,
                FolderId = folderId,
                UserId = _authService.CurrentUser.Id
            };

            _context.Files.Add(file);
            await _context.SaveChangesAsync();
            return file;
        }

        public async Task<List<Folder>> GetRootFoldersAsync()
        {
            if (_authService.CurrentUser == null)
                return new List<Folder>();

            return await _context.Folders
                .Where(f => f.UserId == _authService.CurrentUser.Id && f.ParentFolderId == null)
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .ToListAsync();
        }

        public async Task UpdateFileContentAsync(int fileId, string content)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null && file.UserId == _authService.CurrentUser?.Id)
            {
                file.Content = content;
                file.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteFileAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null && file.UserId == _authService.CurrentUser?.Id)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteFolderAsync(int folderId)
        {
            var folder = await _context.Folders
                .Include(f => f.SubFolders)
                .Include(f => f.Files)
                .FirstOrDefaultAsync(f => f.Id == folderId);

            if (folder != null && folder.UserId == _authService.CurrentUser?.Id)
            {
                _context.Folders.Remove(folder);
                await _context.SaveChangesAsync();
            }
        }
    }
}