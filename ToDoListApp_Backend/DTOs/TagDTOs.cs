using System.ComponentModel.DataAnnotations;

namespace ToDoListApp_Backend.DTOs
{
    public class CreateTagRequest
    {
        [Required]
        public string TagName { get; set; } = string.Empty;
    }

    public class UpdateTagRequest
    {
        [Required]
        public string TagName { get; set; } = string.Empty;
    }

    public class TagResponse
    {
        public int TagId { get; set; }
        public string TagName { get; set; } = string.Empty;
    }
}