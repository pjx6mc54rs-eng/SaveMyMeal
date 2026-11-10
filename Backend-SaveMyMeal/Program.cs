using Microsoft.EntityFrameworkCore;
using Backend_SaveMyMeal.Data;

var builder = WebApplication.CreateBuilder(args);

// Connexion Oracle pour EF Core
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseOracle(builder.Configuration.GetConnectionString("OracleDb")));

builder.Services.AddControllers();
// ... reste de votre code