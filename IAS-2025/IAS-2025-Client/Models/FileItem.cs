namespace IAS_2025_Client.Models
{
    public class FileItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int FolderId { get; set; }
        public int UserId { get; set; }
        public string Extension { get; set; } = ".ias";
        public System.DateTime CreatedAt { get; set; } = System.DateTime.Now;
        public System.DateTime ModifiedAt { get; set; } = System.DateTime.Now;

        // Navigation properties
        public virtual Folder? Folder { get; set; }
        public virtual User? User { get; set; }
    }
}