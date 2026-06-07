using _10Pearls_Web_Project.Server.DBContext;
using _10Pearls_Web_Project.Server.Hubs;
using _10Pearls_Web_Project.Server.Middleware;
using _10Pearls_Web_Project.Server.Models;
using _10Pearls_Web_Project.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

// Bootstrap logger for startup errors only
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog — read full config from appsettings.json
    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext());

    // CORS — explicit headers and methods only; AllowAny* is overbroad for production
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("SignalRPolicy", policy =>
            policy.WithOrigins("https://localhost:7633", "http://localhost:7633")
                  .WithHeaders("Content-Type", "Authorization", "X-Requested-With")
                  .WithMethods("GET", "POST", "PUT", "DELETE")
                  .AllowCredentials());
    });

    // Rate limiting — cap login attempts at 5 per IP per minute
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("login", opt =>
        {
            opt.PermitLimit             = 5;
            opt.Window                  = TimeSpan.FromMinutes(1);
            opt.QueueProcessingOrder    = QueueProcessingOrder.OldestFirst;
            opt.QueueLimit              = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Controllers — serialize enums as strings in JSON responses
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // DB
    builder.Services.AddDbContext<ApplicationDBContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<ApplicationDBContext>()
        .AddDefaultTokenProviders();

    // JWT — AddIdentity above overwrites the default schemes with cookies.
    // Re-declare AddAuthentication here to explicitly restore JWT as the default.
    // AddJwtBearer is additive and does not conflict.
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"] is { Length: > 0 } key
                        ? key
                        : throw new InvalidOperationException("Jwt:Key is not configured or empty"))),
            // Explicitly map claim types so User.IsInRole() and ClaimTypes.Name work correctly
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // Resolve the JWT from either an httpOnly cookie (browsers) or the SignalR
        // query-string token (WebSocket transport / non-browser clients).
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var path = ctx.HttpContext.Request.Path;
                if (path.StartsWithSegments("/hubs"))
                {
                    // SignalR: query-string token takes priority (non-browser clients)
                    var qs = ctx.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(qs))
                    {
                        ctx.Token = qs;
                        return System.Threading.Tasks.Task.CompletedTask;
                    }
                }
                // httpOnly cookie — works for both regular HTTP and hub negotiation
                if (ctx.Request.Cookies.TryGetValue("auth-token", out var cookie))
                    ctx.Token = cookie;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    builder.Services.AddScoped<JWTService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<ITaskService, TaskService>();
    builder.Services.AddScoped<ITaskExportService, TaskExportService>();
    builder.Services.AddScoped<IAdminService, AdminService>();
    builder.Services.AddScoped<IProfileService, ProfileService>();

    // SignalR — must have its own JsonStringEnumConverter because it uses
    // a separate serializer pipeline from AddControllers()
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(
                new System.Text.Json.Serialization.JsonStringEnumConverter());
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Seed roles and default admin on startup
    using (var scope = app.Services.CreateScope())
    {
        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
    }

    // Global exception handler — must be first
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseCors("SignalRPolicy");
    app.UseRateLimiter();

    // Serilog HTTP request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseDefaultFiles();
    app.MapStaticAssets();
    app.MapControllers();
    app.MapHub<TaskHub>("/hubs/tasks");
    app.MapFallbackToFile("/index.html");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
