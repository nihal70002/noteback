using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Note.Backend.Data;
using Note.Backend.Models;
using Note.Backend.Services;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Enable Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// PostgreSQL Database connection
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? Environment.GetEnvironmentVariable("DATABASE_URL");

if (string.IsNullOrEmpty(rawConnectionString))
{
    throw new InvalidOperationException("No database connection string found. Set ConnectionStrings__DefaultConnection or DATABASE_URL environment variable.");
}

// Convert PostgreSQL URI format (postgresql://...) to Npgsql format if needed
var connectionString = ConvertPostgresUriToNpgsql(rawConnectionString);

builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseNpgsql(connectionString));

static string ConvertPostgresUriToNpgsql(string connectionString)
{
    // If it's already a key-value format, return as-is
    if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    // Parse postgresql://username:password@host:port/database
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var username = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// JWT setup
var jwtKey = builder.Configuration["Jwt:Key"]
             ?? "default_super_secret_key_needs_to_be_long_and_secure_12345!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey =
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// Enable CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://note-ocm6.vercel.app",
            "https://note-amber-omega.vercel.app",
            "https://papercues.in",
            "https://www.papercues.in"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // 🔥 REQUIRED
    });
});

var app = builder.Build();

// Configure HTTP request pipeline
app.UseSwagger();
app.UseSwaggerUI();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NoteDbContext>();
    db.Database.Migrate();
    
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders"
        ADD COLUMN IF NOT EXISTS "AlternatePhoneNumber" text NOT NULL DEFAULT '';
    """);
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders"
        ADD COLUMN IF NOT EXISTS "AddressLine1" text NOT NULL DEFAULT '';
    """);
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders"  
        ADD COLUMN IF NOT EXISTS "AddressLine2" text NOT NULL DEFAULT '';
    """);
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders"
        ADD COLUMN IF NOT EXISTS "City" text NOT NULL DEFAULT '';
    """);
    db.Database.ExecuteSqlRaw("""
        ALTER TABLE "Orders"
        ADD COLUMN IF NOT EXISTS "State" text NOT NULL DEFAULT '';
    """);
}

app.UseCors("AllowFrontend");

app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.Headers["Access-Control-Allow-Origin"] = context.Request.Headers["Origin"];
        context.Response.Headers["Access-Control-Allow-Headers"] = "*";
        context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,PUT,DELETE,OPTIONS";
        context.Response.StatusCode = 200;
        return;
    }

    await next();
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();