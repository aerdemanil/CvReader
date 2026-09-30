using CvReader.Api.Data;
using CvReader.Api.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using static FastEndpoints.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddSingleton<CvParserService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
