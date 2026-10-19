
using MathTestAssessment.Api;
using MathTestAssessment.BackgroundJobs;
using MathTestAssessment.Modules.Analytics;
using MathTestAssessment.Modules.Examinations;
using MathTestAssessment.Modules.Identity;
using MathTestAssessment.Modules.Imports;
using MathTestAssessment.Modules.Students;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// OpenAPI
builder.Services.AddOpenApi();

// Application modules
builder.Services.AddExaminationsModule(builder.Configuration);
builder.Services.AddStudentsModule(builder.Configuration);
builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddBackgroundJobsModule(builder.Configuration);
builder.Services.AddImportsModule();
builder.Services.AddAnalyticsModule();

// Background workers
builder.Services.AddApiBackgroundJobs();

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger + JWT
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MathTestAssessment API",
        Version = "v1",
        Description = "Exam management, XML imports and analytics API"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT access token."
    });

    options.AddSecurityRequirement(document =>
     new OpenApiSecurityRequirement
     {
        {
            new OpenApiSecuritySchemeReference(
                "Bearer",
                document),
            new List<string>()
        }
     });
});

var app = builder.Build();

// Create or update all module schemas before serving requests, then seed roles
// and the initial Admin account. MigrateAsync is a no operation when the database is current.
await app.InitializeDatabaseAsync();

// Swagger / OpenAPI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

// A temporary password may authenticate a user, but it cannot authorize normal
// application work until the owner replaces it.
app.Use(async (context, next) =>
{
    var requiresChange = context.User.Identity?.IsAuthenticated == true &&
        string.Equals(context.User.FindFirst("MustChangePassword")?.Value, "true", StringComparison.OrdinalIgnoreCase);
    var allowedPath = context.Request.Path.StartsWithSegments("/api/auth/change-password") ||
        context.Request.Path.StartsWithSegments("/api/auth/logout");

    if (requiresChange && !allowedPath)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = "You must change your temporary password before using the application."
        });
        return;
    }

    await next();
});
app.UseAuthorization();

app.MapControllers();

app.Run();

