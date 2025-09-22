using Microsoft.AspNetCore.Mvc;

namespace ToDoListApp_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public TestController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
        }

        [HttpGet("config")]
        public IActionResult GetConfig()
        {
            return Ok(new
            {
                Environment = _environment.EnvironmentName,
                ContentRootPath = _environment.ContentRootPath,
                WebRootPath = _environment.WebRootPath,

                // AWS Configuration
                AWS_Region = _configuration["AWS:Region"],
                AWS_AccessKey = _configuration["AWS:AccessKey"],
                AWS_SecretKey = !string.IsNullOrEmpty(_configuration["AWS:SecretKey"]) ? "***HIDDEN***" : "NULL",

                // Cognito Configuration  
                Cognito_UserPoolId = _configuration["AWS:Cognito:UserPoolId"],
                Cognito_ClientId = _configuration["AWS:Cognito:ClientId"],
                Cognito_ClientSecret = !string.IsNullOrEmpty(_configuration["AWS:Cognito:ClientSecret"]) ? "***HIDDEN***" : "NULL",
                Cognito_Authority = _configuration["AWS:Cognito:Authority"],

                // JWT Configuration
                JWT_Issuer = _configuration["JWT:Issuer"],
                JWT_Audience = _configuration["JWT:Audience"],

                // Connection String
                ConnectionString = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection")) ? "***CONFIGURED***" : "NULL",

                // All AWS related environment variables
                EnvironmentVariables = Environment.GetEnvironmentVariables()
                    .Cast<System.Collections.DictionaryEntry>()
                    .Where(x => x.Key.ToString().StartsWith("AWS"))
                    .ToDictionary(x => x.Key.ToString(), x => x.Key.ToString().Contains("Secret") ? "***HIDDEN***" : x.Value.ToString())
            });
        }

        [HttpGet("files")]
        public IActionResult ListFiles()
        {
            try
            {
                var contentRootFiles = Directory.GetFiles(_environment.ContentRootPath)
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                var currentDirFiles = Directory.GetFiles(Directory.GetCurrentDirectory())
                    .Select(f => Path.GetFileName(f))
                    .ToList();

                return Ok(new
                {
                    ContentRootPath = _environment.ContentRootPath,
                    CurrentDirectory = Directory.GetCurrentDirectory(),
                    ContentRootFiles = contentRootFiles,
                    CurrentDirectoryFiles = currentDirFiles,
                    EnvFileExists = new
                    {
                        InContentRoot = System.IO.File.Exists(Path.Combine(_environment.ContentRootPath, ".env")),
                        InCurrentDir = System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), ".env"))
                    }
                });
            }
            catch (Exception ex)
            {
                return Ok(new { Error = ex.Message });
            }
        }
    }
}