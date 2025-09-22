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
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        var envPath = Path.Combine(env.ContentRootPath, ".env");

        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }
        else
        {
            var currentEnvPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(currentEnvPath))
            {
                Env.Load(currentEnvPath);
            }
        }

        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables();

        Configuration = builder.Build();
    }

    public IConfiguration Configuration { get; }

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

        // Đọc connection string từ environment variable
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        services.AddDbContext<DbtodolistappContext>(options =>
            options.UseMySql(
                connectionString,
                ServerVersion.AutoDetect(connectionString)
            ));

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
                // Đọc JWT settings từ environment variables
                var jwtIssuer = Environment.GetEnvironmentVariable("JWT__Issuer");
                var jwtAudience = Environment.GetEnvironmentVariable("JWT__Audience");
                var cognitoAuthority = Environment.GetEnvironmentVariable("AWS__Cognito__Authority");

                options.Authority = cognitoAuthority;
                options.Audience = jwtAudience;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,

                    AudienceValidator = (audiences, securityToken, validationParameters) =>
                    {
                        if (securityToken is JsonWebToken jwt)
                        {
                            var clientId = jwt.Claims?.FirstOrDefault(c => c.Type == "client_id")?.Value;
                            return clientId == jwtAudience;
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

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseRouting();
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