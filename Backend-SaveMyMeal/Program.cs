using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.IdentityModel.Tokens;
using Backend_SaveMyMeal.Data;
using Backend_SaveMyMeal.Models.Entities;
using Backend_SaveMyMeal.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Connexion Oracle pour EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

// 2. Enregistrement des services métier (Business Logic)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISmartDiscountService, SmartDiscountService>();
builder.Services.AddScoped<IMealService, MealService>();
builder.Services.AddScoped<ILoyaltyService, LoyaltyService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 3. Configuration de l'authentification JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SaveMyMealSuperSecretFallbackKey2026!#";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "SaveMyMealBackend";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "SaveMyMealApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 4. Configuration des contrôleurs et de la sérialisation JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 5. Configuration CORS (pour l'application mobile .NET MAUI et frontend web)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 6. Documentation OpenAPI / Swagger
builder.Services.AddOpenApi();

var app = builder.Build();

// 7. Initialisation de la base Oracle & Création des tables si absentes
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        var creator = db.Database.GetService<IRelationalDatabaseCreator>();
        try
        {
            creator.CreateTables();
            logger.LogInformation("Les tables Oracle ont été créées avec succès.");
        }
        catch
        {
            // Les tables existent déjà, rien à faire
        }

        // Seed initial des catégories de repas si absentes
        if (!db.Categories.Any())
        {
            logger.LogInformation("Insertion des catégories initiales...");
            db.Categories.AddRange(
                new Category { Name = "Boulangerie & Pâtisserie" },
                new Category { Name = "Plats Cuisinés & Traiteur" },
                new Category { Name = "Paniers Surprise" },
                new Category { Name = "Sushi & Cuisine Asiatique" },
                new Category { Name = "Pizzas & Sandwiches" }
            );
            db.SaveChanges();
            logger.LogInformation("Catégories insérées avec succès.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erreur lors de l'initialisation automatique d'Oracle.");
    }
}

// Configuration du pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de bienvenue et statut de santé de l'API
app.MapGet("/", () => Results.Ok(new
{
    application = "SaveMyMeal API",
    status = "Online",
    version = "1.0",
    description = "Plateforme anti-gaspillage alimentaire connectant commerçants et consommateurs.",
    documentation = "/openapi/v1.json"
}));

app.Run();
