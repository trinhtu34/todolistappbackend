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
    public class TagsController : ControllerBase
    {
        private readonly DbtodolistappContext _context;
        private readonly ILogger<TagsController> _logger;

        public TagsController(DbtodolistappContext context, ILogger<TagsController> logger)
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
        public async Task<ActionResult<IEnumerable<TagResponse>>> GetTags()
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var tags = await _context.Tags
                    .Where(t => t.CognitoSub == cognitoSub)
                    .Select(t => new TagResponse
                    {
                        TagId = t.TagId,
                        TagName = t.TagName
                    })
                    .ToListAsync();

                return Ok(tags);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tags");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TagResponse>> GetTag(int id)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var tag = await _context.Tags
                    .Where(t => t.TagId == id && t.CognitoSub == cognitoSub)
                    .Select(t => new TagResponse
                    {
                        TagId = t.TagId,
                        TagName = t.TagName
                    })
                    .FirstOrDefaultAsync();

                if (tag == null)
                {
                    return NotFound();
                }

                return Ok(tag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tag {TagId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<TagResponse>> CreateTag([FromBody] CreateTagRequest request)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var tag = new Tag
                {
                    TagName = request.TagName,
                    CognitoSub = cognitoSub
                };

                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                var response = new TagResponse
                {
                    TagId = tag.TagId,
                    TagName = tag.TagName
                };

                return CreatedAtAction(nameof(GetTag), new { id = tag.TagId }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTag(int id, [FromBody] UpdateTagRequest request)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.TagId == id && t.CognitoSub == cognitoSub);

                if (tag == null)
                {
                    return NotFound();
                }

                tag.TagName = request.TagName;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag {TagId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTag(int id)
        {
            try
            {
                var cognitoSub = GetCognitoSub();
                if (string.IsNullOrEmpty(cognitoSub))
                {
                    return Unauthorized();
                }

                var tag = await _context.Tags
                    .FirstOrDefaultAsync(t => t.TagId == id && t.CognitoSub == cognitoSub);

                if (tag == null)
                {
                    return NotFound();
                }

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag {TagId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}