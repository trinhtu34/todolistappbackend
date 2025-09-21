using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoListApp_Backend.Models
{
    [Table("todos")]
    public class Todo
    {
        [Key]
        [Column("todo_id")]
        public int TodoId { get; set; }

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("is_done")]
        public bool IsDone { get; set; } = false;

        [Column("due_date")]
        public DateTime? DueDate { get; set; }

        [Column("create_at")]
        public DateTime CreateAt { get; set; } = DateTime.Now;

        [Column("update_at")]
        public DateTime UpdateAt { get; set; } = DateTime.Now;

        [Column("cognito_sub")]
        public string CognitoSub { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}