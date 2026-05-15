using Microsoft.EntityFrameworkCore;
using SimpleIDE.Data;
using SimpleIDE.Models;

namespace SimpleIDE.Services
{
    public class AuthService
    {
        private User? _currentUser;

        public User? CurrentUser => _currentUser;
        public bool IsAdmin => _currentUser?.IsAdmin ?? false;

        private ApplicationDbContext CreateContext()
        {
            return new ApplicationDbContext();
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            using var context = CreateContext();

            if (await context.Users.AnyAsync(u => u.Username == username))
                return false;

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdmin = false,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            using var context = CreateContext();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null) return false;

            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                _currentUser = user;
                return true;
            }

            return false;
        }

        public async Task RefreshCurrentUserAsync()
        {
            if (_currentUser == null) return;

            using var context = CreateContext();
            var freshUser = await context.Users.FindAsync(_currentUser.Id);
            if (freshUser != null)
            {
                _currentUser = freshUser;
            }
        }

        public async Task<bool> MakeAdminAsync(string password, int userId)
        {
            if (password != "AdminPass123!") return false;

            using var context = CreateContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsAdmin = true;
            await context.SaveChangesAsync();

            if (_currentUser?.Id == userId)
                _currentUser.IsAdmin = true;

            return true;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context = CreateContext();
            return await context.Users.ToListAsync();
        }

        public async Task<bool> SetAdminRoleAsync(int userId, bool isAdmin)
        {
            if (!IsAdmin) return false;

            using var context = CreateContext();
            var user = await context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsAdmin = isAdmin;
            await context.SaveChangesAsync();
            return true;
        }

        public void Logout()
        {
            _currentUser = null;
        }
    }
}