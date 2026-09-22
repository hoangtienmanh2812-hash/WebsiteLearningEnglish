using EnglishLearning.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Khởi tạo các services từ Infrastructure (EF Core, Database...)
builder.Services.AddInfrastructure(builder.Configuration);

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Dùng CORS policy đã định nghĩa
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
