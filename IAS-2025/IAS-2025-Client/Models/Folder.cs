using IAS;
using System.Collections.Generic;

namespace IAS_2025_Client.Models
{
    public class Folder
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ParentFolderId { get; set; }
        public int UserId { get; set; }
        public string Path { get; set; } = string.Empty;
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;

        // Navigation properties
        public virtual User? User { get; set; }
        public virtual Folder? ParentFolder { get; set; }
        public virtual ICollection<Folder> SubFolders { get; set; } = new List<Folder>();
        public virtual ICollection<FileItem> Files { get; set; } = new List<FileItem>();
    }
}