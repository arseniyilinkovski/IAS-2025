using System.ComponentModel.DataAnnotations;
using System.Windows.Controls;

namespace SimpleIDE.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
        public virtual ICollection<Folder> Folders { get; set; } = new List<Folder>();
    }
}