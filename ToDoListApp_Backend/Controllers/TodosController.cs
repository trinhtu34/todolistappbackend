using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using ToDoListApp_Backend.DTOs;
using ToDoListApp_Backend.Models;

namespace ToDoListApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TodosController : ControllerBase
    {
        private readonly DbtodolistappContext _context;
        private readonly ILogger<TodosController> _logger;

        public TodosController(DbtodolistappContext context, ILogger<TodosController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private string GetCognitoSub()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (authHeader != null && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadJwtToken(token);
                return jsonToken.Claims.FirstOrDefault(x => x.Type == "sub")?.Value ?? string.Empty;
            }
            return string.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos()
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var todos = await _context.Todos
                    .Where(t => t.CognitoSub == cognitoSub)
                    .Include(t => t.Tags)
                    .Select(t => new TodoResponse
                    {
                        TodoId = t.TodoId,
                        Description = t.Description,
                        IsDone = t.IsDone,
                        DueDate = t.DueDate,
                        CreateAt = t.CreateAt,
                        UpdateAt = t.UpdateAt,
                        Tags = t.Tags.Select(tag => new TagResponse
                        {
                            TagId = tag.TagId,
                            TagName = tag.TagName
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(todos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todos");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TodoResponse>> GetTodo(int id)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var todo = await _context.Todos
                    .Where(t => t.TodoId == id && t.CognitoSub == cognitoSub)
                    .Include(t => t.Tags)
                    .Select(t => new TodoResponse
                    {
                        TodoId = t.TodoId,
                        Description = t.Description,
                        IsDone = t.IsDone,
                        DueDate = t.DueDate,
                        CreateAt = t.CreateAt,
                        UpdateAt = t.UpdateAt,
                        Tags = t.Tags.Select(tag => new TagResponse
                        {
                            TagId = tag.TagId,
                            TagName = tag.TagName
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (todo == null)
                {
                    return NotFound();
                }

                return Ok(todo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting todo {TodoId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TodoResponse>> CreateTodo([FromBody] CreateTodoRequest request)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var todo = new Todo
                {
                    Description = request.Description,
                    DueDate = request.DueDate,
                    CognitoSub = cognitoSub,
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now
                };

                _context.Todos.Add(todo);
                await _context.SaveChangesAsync();

                // Add tags if provided
                if (request.TagIds.Any())
                {
                    var validTags = await _context.Tags
                        .Where(t => request.TagIds.Contains(t.TagId) && t.CognitoSub == cognitoSub)
                        .ToListAsync();

                    todo.Tags = validTags;
                    await _context.SaveChangesAsync();
                }

                // Reload todo with tags
                var createdTodo = await _context.Todos
                    .Where(t => t.TodoId == todo.TodoId)
                    .Include(t => t.Tags)
                    .Select(t => new TodoResponse
                    {
                        TodoId = t.TodoId,
                        Description = t.Description,
                        IsDone = t.IsDone,
                        DueDate = t.DueDate,
                        CreateAt = t.CreateAt,
                        UpdateAt = t.UpdateAt,
                        Tags = t.Tags.Select(tag => new TagResponse
                        {
                            TagId = tag.TagId,
                            TagName = tag.TagName
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                return CreatedAtAction(nameof(GetTodo), new { id = todo.TodoId }, createdTodo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating todo");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTodo(int id, [FromBody] UpdateTodoRequest request)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var todo = await _context.Todos
                    .Include(t => t.Tags)
                    .FirstOrDefaultAsync(t => t.TodoId == id && t.CognitoSub == cognitoSub);

                if (todo == null)
                {
                    return NotFound();
                }

                // Update fields if provided
                if (!string.IsNullOrEmpty(request.Description))
                {
                    todo.Description = request.Description;
                }

                if (request.IsDone.HasValue)
                {
                    todo.IsDone = request.IsDone.Value;
                }

                if (request.DueDate.HasValue)
                {
                    todo.DueDate = request.DueDate.Value;
                }

                todo.UpdateAt = DateTime.Now;

                // Update tags if provided
                if (request.TagIds != null)
                {
                    // Clear existing tags
                    todo.Tags.Clear();

                    // Add new tags
                    if (request.TagIds.Any())
                    {
                        var validTags = await _context.Tags
                            .Where(t => request.TagIds.Contains(t.TagId) && t.CognitoSub == cognitoSub)
                            .ToListAsync();

                        foreach (var tag in validTags)
                        {
                            todo.Tags.Add(tag);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating todo {TodoId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var todo = await _context.Todos
                    .FirstOrDefaultAsync(t => t.TodoId == id && t.CognitoSub == cognitoSub);

                if (todo == null)
                {
                    return NotFound();
                }

                _context.Todos.Remove(todo);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting todo {TodoId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}