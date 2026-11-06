using CollegeApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Строка подключения к базе данных колледжа
var connectionString = "Data Source=stud-mssql.sttec.yar.ru,38325;Initial Catalog=user111_db;User ID=user111_db;Password=user111;TrustServerCertificate=True;Encrypt=False;";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();

// Ошибки базы данных и прочие исключения возвращаем клиенту в понятном виде
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new { message = "Ошибка сервера: " + ex.Message });
    }
});

app.MapControllers();
app.Run();
