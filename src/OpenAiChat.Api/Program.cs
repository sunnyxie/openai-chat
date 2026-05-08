using Microsoft.OpenApi.Models;
using OpenAiChat.Api.Configuration;
using OpenAiChat.Api.Middleware;
using OpenAiChat.Api.Services;
using Polly.Registry;


var builder = WebApplication.CreateBuilder(args);

// ── Configuration ─────────────────────────────────────────────────────────────

// Bind strongly-typed options from appsettings.json (and environment overrides)
builder.Services
    .AddOptions<OpenAiOptions>()
    .BindConfiguration(OpenAiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ResiliencePolicyOptions>()
    .BindConfiguration(ResiliencePolicyOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ── Logging ───────────────────────────────────────────────────────────────────
// ASP.NET Core wires up Console + Debug providers by default.
// Override with appsettings.json "Logging" section as needed.
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Polly policy registry ─────────────────────────────────────────────────────

// Resolve ResiliencePolicyOptions eagerly so we can build policies at startup
var resilienceOpts = builder.Configuration
    .GetSection(ResiliencePolicyOptions.SectionName)
    .Get<ResiliencePolicyOptions>() ?? new ResiliencePolicyOptions();

// We need a logger for the policy registry — build a minimal logger factory
var startupLoggerFactory = LoggerFactory.Create(b => b.AddConsole());
var policyLogger = startupLoggerFactory.CreateLogger("ResiliencePolicies");

var policyRegistry = new PolicyRegistry();
ResiliencePolicies.RegisterPolicies(policyRegistry, resilienceOpts, policyLogger);


// Register the registry as a singleton so it can be injected anywhere
builder.Services.AddSingleton<IReadOnlyPolicyRegistry<string>>(policyRegistry);
builder.Services.AddSingleton<IPolicyRegistry<string>>(policyRegistry);

// ── Application services ──────────────────────────────────────────────────────

builder.Services.AddScoped<IChatService, OpenAiChatService>();

// ── Controllers ───────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// ── Swagger / OpenAPI ─────────────────────────────────────────────────────────

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "OpenAI Chat API",
        Version     = "v1",
        Description = "ASP.NET Core 8 proxy that forwards chat messages to OpenAI."
    });

    // Include XML doc comments in Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});


// ── CORS (allow the Vite dev server) ─────────────────────────────────────────

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy
            .WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ══════════════════════════════════════════════════════════════════════════════
//  Middleware pipelines
// ══════════════════════════════════════════════════════════════════════════════

// Add health endpoint
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");

// Global exception handler must be first in the pipeline
app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OpenAI Chat API v1");
        c.RoutePrefix = string.Empty; // Swagger at root /
    });
}

app.UseHttpsRedirection();
app.UseCors("ViteDev");
app.UseAuthorization();
app.MapControllers();

// json settings validation. (testing)
var resilienceSection = builder.Configuration.GetSection("ResiliencePolicy");
Console.WriteLine($"dbug: RetryCount: {resilienceSection["RetryCount"]}");

app.Run();

// Make the implicit Program class accessible to the test project
public partial class Program { }
