using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SimpleIDE.Models
{
    public class Template
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        public bool IsSystem { get; set; } = false; // Системные шаблоны нельзя удалить

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}