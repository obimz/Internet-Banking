using Internet_Banking.Data;
using Internet_Banking.Extensions;
using Internet_Banking.Models;
using Internet_Banking.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }); //enables enum values to accept string values in the request body.

builder.Services.AddOpenApi();

builder.Services.AddDbContext<Internet_BankingDbContext>(option => 
option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IPasswordHasher<Customer>, PasswordHasher<Customer>>();

builder.Services.AddScoped<IAccountStatementService, AccountStatementService>();//Registers the AccountStatementService with the dependency injection container.

builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddScoped<ITransactionService, TransactionService>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme) //registers the authentication services and sets the default authentication scheme to JWT Bearer
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey (Encoding.UTF8.GetBytes(jwtKey)),

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1), // to account for any clock differences between the server and the client

            RoleClaimType = ClaimTypes.Role // tells JWT middleware which claim holds for user's role
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));//registers the adminOnly policy

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Internet Banking API v1");
    });
    await app.SeedDatabaseAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();//reads and validates the Jwt tokens

app.UseAuthorization();//checks the adminOnly policy

app.MapControllers();

app.Run();
