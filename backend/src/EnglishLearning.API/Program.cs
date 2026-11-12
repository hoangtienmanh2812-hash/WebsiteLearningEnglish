using EnglishLearning.Application.Features.Chatbot.Interfaces;
using EnglishLearning.Infrastructure;
using EnglishLearning.Infrastructure.Chatbot;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

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

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();