using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TaskManager.Api.Middleware;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;
using TaskManager.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// ---- Application + Infrastructure (composition root) ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Controllers / JSON ----
builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        // Serialize enums as their names ("Todo", "InProgress", "Done") for readable, stable contracts.
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(o =>
{
    // Let FluentValidation (in the Application layer) own business validation, rather than
    // ASP.NET treating every non-nullable reference property as implicitly [Required].
    o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

// ---- Authentication / Authorization (JWT bearer) ----
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey))
    throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // Keep claim types as issued (sub, NameIdentifier, email).
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ---- CORS (allow the Blazor WASM client) ----
const string SpaCorsPolicy = "spa";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(SpaCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// ---- Swagger with bearer support ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Task Manager API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT obtained from /api/auth/login.",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });
});

var app = builder.Build();

// ---- Initialize + seed the database on startup ----
await app.InitializeDatabaseAsync();

// ---- HTTP pipeline ----
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(SpaCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Startup helper kept local to Program for clarity.
internal static class StartupExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        await sp.GetRequiredService<IDatabaseInitializer>().InitializeAsync();
        await sp.GetRequiredService<DataSeeder>().SeedAsync();
    }
}

// Exposed for integration testing via WebApplicationFactory<Program>.
public partial class Program;
