using IAS;
using System;
using System.Collections.Generic;

namespace IAS_2025_Client.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        // Navigation properties
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
    }
}