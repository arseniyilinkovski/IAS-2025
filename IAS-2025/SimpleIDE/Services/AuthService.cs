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
        public static (bool IsValid, string Message) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Пароль не может быть пустым");

            if (password.Length < 6)
                return (false, "Пароль должен содержать минимум 6 символов");

            if (password.Length > 50)
                return (false, "Пароль не должен превышать 50 символов");

            bool hasDigit = password.Any(char.IsDigit);
            bool hasUpper = password.Any(char.IsUpper);
            bool hasLower = password.Any(char.IsLower);

            // Собираем требования
            var missingRequirements = new List<string>();

            if (!hasDigit) missingRequirements.Add("цифру");
            if (!hasUpper) missingRequirements.Add("заглавную букву");
            if (!hasLower) missingRequirements.Add("строчную букву");

            if (missingRequirements.Any())
            {
                string message = $"Пароль должен содержать: {string.Join(", ", missingRequirements)}";
                return (false, message);
            }

            return (true, "Пароль надежный");
        }

        public async Task<(bool Success, string Message)> RegisterAsync(string username, string password)
        {
            var passwordValidation = ValidatePassword(password);
            if (!passwordValidation.IsValid)
                return (false, passwordValidation.Message);

            using var context = CreateContext();

            if (await context.Users.AnyAsync(u => u.Username == username))
                return (false, "Пользователь с таким именем уже существует");

            var user = new User
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                IsAdmin = false,
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return (true, "Регистрация успешна!");
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