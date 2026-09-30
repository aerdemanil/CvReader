using CvReader.Application;
using CvReader.Infrastructure;
using FastEndpoints;
using static FastEndpoints.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
