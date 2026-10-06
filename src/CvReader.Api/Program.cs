using System.Net;
using CvReader.Api.Auth;
using CvReader.Api.Background;
using CvReader.Api.Security;
using CvReader.Application;
using CvReader.Application.Abstractions;
using CvReader.Application.Auth;
using CvReader.Infrastructure;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using static FastEndpoints.Swagger.Extensions;

var builder = WebApplication.CreateBuilder(args);

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwt = jwtSection.Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
    throw new InvalidOperationException(
        "Jwt:SigningKey is missing or shorter than 32 characters. Set it with 'dotnet user-secrets set \"Jwt:SigningKey\" \"...\"'.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

builder.Services
    .AddAuthenticationJwtBearer(s => s.SigningKey = jwt.SigningKey, bearer =>
    {
        bearer.TokenValidationParameters.ValidateIssuer = true;
        bearer.TokenValidationParameters.ValidIssuer = jwt.Issuer;
        bearer.TokenValidationParameters.ValidateAudience = true;
        bearer.TokenValidationParameters.ValidAudience = jwt.Audience;

        bearer.Events = new JwtBearerEvents
        {
            // Tarayıcı token'ı HttpOnly cookie ile gönderir; Authorization header'ı da çalışmaya devam eder.
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(AuthCookie.Name, out var token))
                    context.Token = token;
                return Task.CompletedTask;
            },
            // İmzası geçerli olsa da çıkış yapılmış ya da kullanıcısı silinmiş bir token kabul edilmez.
            OnTokenValidated = async context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<AuthService>();

                if (!context.Principal!.TryGetSession(out var userId, out var tokenVersion)
                    || !await authService.IsSessionValidAsync(userId, tokenVersion, context.HttpContext.RequestAborted))
                    context.Fail("The session is no longer valid.");
            }
        };
    })
    .AddAuthorization()
    .AddRateLimits()
    .AddProblemDetails()
    .AddFastEndpoints();

// Ters vekil arkasında gerçek istemci IP'si X-Forwarded-For'dan okunur; yalnızca bilinen vekillere güvenilir.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        options.KnownProxies.Add(IPAddress.Parse(proxy));
});

if (builder.Environment.IsDevelopment())
    builder.Services.SwaggerDocument();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<CvFieldExtractionWorker>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseSecurityHeaders();
if (!app.Environment.IsDevelopment())
{
    // Beklenmeyen hatalar loglanır; istemciye ayrıntı içermeyen bir 500 yanıtı döner.
    app.UseExceptionHandler();
    app.UseHsts();
    app.UseHttpsRedirection();
}

// React ön yüzünün build çıktısı (wwwroot) kök adreste sunulur.
app.UseDefaultFiles();
app.UseStaticFiles();

// Hız sınırı kullanıcıya göre bölümlendiği için kimlik doğrulamadan sonra çalışır.
app.UseAuthentication()
   .UseAuthorization()
   .UseRateLimiter()
   .UseFastEndpoints();

if (app.Environment.IsDevelopment())
    app.UseSwaggerGen();

app.Run();
