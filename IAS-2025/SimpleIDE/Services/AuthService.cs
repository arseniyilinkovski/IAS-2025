using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private User? _currentUser;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        public User? CurrentUser => _currentUser;
        public bool IsAdmin => _currentUser?.IsAdmin ?? false;

        public async Task<bool> RegisterAsync(string username, string password)
        {
            if (await _context.Users.AnyAsync(u => u.Username == username))
                return false;

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdmin = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return false;

            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _currentUser = user;
                return true;
            }

            return false;
        }

        // Принудительное обновление текущего пользователя из БД
        public async Task RefreshCurrentUserAsync()
        {
            if (_currentUser != null)
            {
                var freshUser = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

                if (freshUser != null)
                {
                    _currentUser = freshUser;
                    System.Diagnostics.Debug.WriteLine($"User {_currentUser.Username} refreshed. IsAdmin: {_currentUser.IsAdmin}");
                }
            }
        }

        // Проверка прав администратора напрямую из БД
        public async Task<bool> IsAdminRealTimeAsync()
        {
            if (_currentUser == null) return false;

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == _currentUser.Id);

            return user?.IsAdmin ?? false;
        }

        public async Task<bool> MakeAdminAsync(string password, int userId)
        {
            if (password != "AdminPass123!") return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsAdmin = true;
            await _context.SaveChangesAsync();

            if (_currentUser?.Id == userId)
            {
                _currentUser.IsAdmin = true;
            }

            return true;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<bool> SetAdminRoleAsync(int userId, bool isAdmin)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsAdmin = isAdmin;
            await _context.SaveChangesAsync();

            // Если меняем текущего пользователя - обновляем его
            if (_currentUser?.Id == userId)
            {
                _currentUser.IsAdmin = isAdmin;
            }

            System.Diagnostics.Debug.WriteLine($"User {user.Username} admin status changed to: {isAdmin}");
            return true;
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public void Logout()
        {
            _currentUser = null;
        }
    }
}