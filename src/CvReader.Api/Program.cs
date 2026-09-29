
using FastEnpoints;
using static FastEnpoints.swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
