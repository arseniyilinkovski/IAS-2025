using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleIDE.Models
{
    public class Folder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Path { get; set; }

        public int? ParentFolderId { get; set; }

        [ForeignKey("ParentFolderId")]
        public virtual Folder? ParentFolder { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
    }
}