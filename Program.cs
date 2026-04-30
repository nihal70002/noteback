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
builder.Services.AddDbContext<NoteDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.Configure<CloudinaryOptions>(builder.Configuration.GetSection("Cloudinary"));
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
            "https://localhost:5173",
            "https://note-ocm6.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
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

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();