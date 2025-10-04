using GastroBotAPI.Context;
using GastroBotAPI.Models;
using GastroBotAPI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<IdentityContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHttpClient<GeminiServices>(client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services
    .AddIdentityApiEndpoints<User>()
    .AddEntityFrameworkStores<IdentityContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapIdentityApi<User>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://*:{port}");