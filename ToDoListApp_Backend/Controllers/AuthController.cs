using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoListApp_Backend.DTOs;
using ToDoListApp_Backend.Services;

namespace ToDoListApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ICognitoService _cognitoService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ICognitoService cognitoService, ILogger<AuthController> logger)
        {
            _cognitoService = cognitoService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _cognitoService.LoginAsync(request.Email, request.Password);

                if (result.IsSuccess)
                {
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        AccessToken = result.AccessToken,
                        IdToken = result.IdToken,
                        RefreshToken = result.RefreshToken,
                        ExpiresIn = result.ExpiresIn
                    });
                }

                return BadRequest(new LoginResponse
                {
                    Success = false,
                    Message = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for user {Email}", request.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var success = await _cognitoService.SignUpAsync(request.Password, request.Name, request.Email);

                if (success)
                {
                    return Ok(new
                    {
                        message = "Registration successful. Please check your email for verification code.",
                        email = request.Email
                    });
                }

                return BadRequest(new { message = "Registration failed. Please check your information and try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration error for user {Email}: {Error}", request.Email, ex.Message);
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}" });
            }
        }

        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmSignUp([FromBody] ConfirmSignUpRequest request)
        {
            try
            {
                var success = await _cognitoService.ConfirmSignUpAsync(request.Email, request.ConfirmationCode);

                if (success)
                {
                    return Ok(new { message = "Email confirmed successfully. You can now login." });
                }

                return BadRequest(new { message = "Email confirmation failed" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email confirmation error for user {Email}", request.Email);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _cognitoService.RefreshTokenAsync(request.RefreshToken);

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        accessToken = result.AccessToken,
                        idToken = result.IdToken,
                        expiresIn = result.ExpiresIn
                    });
                }

                return BadRequest(new { message = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token refresh error");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();
                if (authHeader != null && authHeader.StartsWith("Bearer "))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    var userInfo = await _cognitoService.GetUserInfoAsync(token);

                    if (userInfo != null)
                    {
                        return Ok(userInfo);
                    }
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get profile error");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}