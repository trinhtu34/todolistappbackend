using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ToDoListApp_Backend.Models
{
    [Table("tags")]
    public class Tag
    {
        [Key]
        [Column("tag_id")]
        public int TagId { get; set; }

        [Column("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [Column("cognito_sub")]
        public string CognitoSub { get; set; } = string.Empty;

        // Navigation properties
        public virtual ICollection<Todo> Todos { get; set; } = new List<Todo>();
    }
}