using System.Security.Claims;
using System.Threading.RateLimiting;
using EnglishLearning.Application.Features.Chatbot.Interfaces;
using EnglishLearning.Infrastructure;
using EnglishLearning.Infrastructure.Chatbot;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
var chatbotLimits = builder.Configuration.GetSection("Chatbot:Limits").Get<ChatbotLimits>()
    ?? new ChatbotLimits();
chatbotLimits.Validate();
builder.Services.AddSingleton(chatbotLimits);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT access token."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });
});
builder.Services.AddHttpClient();

// Khởi tạo các services từ Infrastructure (EF Core, Database...)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpClient<IChatbotService, GeminiChatbotService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ChatbotPerUser", context =>
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        var partitionKey = Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId.ToString("N")
            : context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = chatbotLimits.RequestsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { message = "Bạn đã vượt quá số lượt chat cho phép trong phút này. Vui lòng thử lại sau." },
            cancellationToken);
    };
});

// Cấu hình CORS để cho phép Frontend (Vite) gọi API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Port mặc định của Vite
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Nếu cần dùng Cookie/Token
    });
});

var app = builder.Build();

app.UseMiddleware<EnglishLearning.API.SystemErrorLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();