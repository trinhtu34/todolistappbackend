using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using ToDoListApp_Backend.Models;
using ToDoListApp_Backend.Services;

namespace ToDoListApp_Backend;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Env.Load();

        var builder = new ConfigurationBuilder()
            .AddConfiguration(configuration)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.WriteIndented = true;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        // Add Entity Framework with MySQL
        services.AddDbContext<DbtodolistappContext>(options =>
            options.UseMySql(
                Configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(Configuration.GetConnectionString("DefaultConnection"))
            ));

        // Add Cognito Service
        services.AddScoped<ICognitoService, CognitoService>();

        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
            logging.SetMinimumLevel(LogLevel.Information);
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwtSettings = Configuration.GetSection("JWT");
                var awsSettings = Configuration.GetSection("AWS:Cognito");

                options.Authority = awsSettings["Authority"];
                options.Audience = jwtSettings["Audience"];

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,

                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        if (securityToken is JsonWebToken jwt)
                        {
                            var clientId = jwt.Claims?.FirstOrDefault(c => c.Type == "client_id")?.Value;
                            return clientId == jwtSettings["Audience"]; // So sánh client_id với config
                        }
                        return false;
                    }
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine($"Token validated for user: {context.Principal.Identity.Name}");
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        // Add CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll",
                policy =>
                {
                    policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                });
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseHttpsRedirection();

        // Enable CORS - MUST be before UseRouting
        app.UseCors("AllowAll");

        app.UseRouting();

        // Authentication and Authorization MUST be after UseRouting
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("Welcome to running ASP.NET Core on AWS Lambda");
            });
        });
    }
}