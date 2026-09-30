using CvReader.Api.Services;
using FastEndpoints;
using static FastEndpoints.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddSingleton<CvParserService>();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
