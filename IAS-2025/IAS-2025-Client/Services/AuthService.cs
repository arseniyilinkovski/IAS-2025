using IAS_2025_Client.Data;
using IAS_2025_Client.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IAS_2025_Client.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        public User? CurrentUser { get; private set; }

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public async Task<(bool success, string message)> Register(string username, string email, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return (false, "Username is required");

                if (string.IsNullOrWhiteSpace(email))
                    return (false, "Email is required");

                if (string.IsNullOrWhiteSpace(password))
                    return (false, "Password is required");

                if (password.Length < 6)
                    return (false, "Password must be at least 6 characters");

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username || u.Email == email);

                if (existingUser != null)
                {
                    if (existingUser.Username == username)
                        return (false, "Username already exists");
                    return (false, "Email already exists");
                }

                var user = new User
                {
                    Username = username,
                    Email = email,
                    PasswordHash = HashPassword(password),
                    IsAdmin = !_context.Users.Any() // First user becomes admin
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return (true, "Registration successful");
            }
            catch (Exception ex)
            {
                return (false, $"Registration failed: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> Login(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                    return (false, "Invalid username or password");

                if (user.PasswordHash != HashPassword(password))
                    return (false, "Invalid username or password");

                user.LastLogin = DateTime.Now;
                await _context.SaveChangesAsync();

                CurrentUser = user;
                return (true, "Login successful");
            }
            catch (Exception ex)
            {
                return (false, $"Login failed: {ex.Message}");
            }
        }

        public async Task<bool> MakeAdmin(int userId, string adminPassword)
        {
            if (CurrentUser == null || !CurrentUser.IsAdmin)
                return false;

            if (HashPassword(adminPassword) != CurrentUser.PasswordHash)
                return false;

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsAdmin = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<User>> GetAllUsers()
        {
            if (CurrentUser == null || !CurrentUser.IsAdmin)
                return new List<User>();

            return await _context.Users.ToListAsync();
        }

        public void Logout()
        {
            CurrentUser = null;
        }
    }
}