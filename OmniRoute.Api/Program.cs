using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using OmniRoute.Api.Middleware;
using OmniRoute.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Register Controllers and HTTP Services
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Production Remote Database Connection
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=db69200.databaseasp.net;Database=db69200;User Id=db69200;Password=J!o7nB2?Q_s9;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "UpCode SMS API Gateway Engine", Version = "v1" });

    // Enable API Key Authorization Header in Swagger UI
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Enter your secret API Key (Header: X-API-KEY)",
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOmniRouteClients", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 1. CORS सधैं पहिलोमा
app.UseCors("AllowOmniRouteClients");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "UpCode SMS API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// 2. API Key Middleware (UseRouting पछि ताकि endpoints हरू सहि तरिकाले म्याच हुन्)
app.UseMiddleware<ApiKeyAuthMiddleware>();

// Redirect root domain "/" directly to index.html
app.MapGet("/", () => Results.File("index.html", "text/html"));

app.MapControllers();
app.Run();