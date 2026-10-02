using CvReader.Api.Auth;
using CvReader.Application;
using CvReader.Application.Abstractions;
using CvReader.Infrastructure;
using FastEndpoints;
using FastEndpoints.Security;
using static FastEndpoints.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var signingKey = jwtSection["SigningKey"];
if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:SigningKey is missing or shorter than 32 characters. Set it with 'dotnet user-secrets set \"Jwt:SigningKey\" \"...\"'.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services
    .AddAuthenticationJwtBearer(s => s.SigningKey = signingKey)
    .AddAuthorization()
    .AddFastEndpoints();
builder.Services.SwaggerDocument();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.UseAuthentication()
   .UseAuthorization()
   .UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
