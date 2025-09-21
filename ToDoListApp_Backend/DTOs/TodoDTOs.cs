using System.ComponentModel.DataAnnotations;

namespace ToDoListApp_Backend.DTOs
{
    public class CreateTodoRequest
    {
        [Required]
        public string Description { get; set; } = string.Empty;
        
        public DateTime? DueDate { get; set; }
        
        public List<int> TagIds { get; set; } = new List<int>();
    }

    public class UpdateTodoRequest
    {
        public string? Description { get; set; }
        
        public bool? IsDone { get; set; }
        
        public DateTime? DueDate { get; set; }
        
        public List<int>? TagIds { get; set; }
    }

    public class TodoResponse
    {
        public int TodoId { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsDone { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public List<TagResponse> Tags { get; set; } = new List<TagResponse>();
    }
}