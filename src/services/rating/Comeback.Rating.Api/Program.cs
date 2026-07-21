using System.Text;
using Comeback.Rating.Application;
using Comeback.Rating.Domain;
using Comeback.Rating.Infrastructure;
using Comeback.Rating.Api.Endpoints.Rating;
using Comeback.BuildingBlocks.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console()
        .WriteTo.Seq(ctx.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341"));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

    builder.Services.AddBuildingBlocks(
        ApplicationAssembly.Assembly,
        DomainAssembly.Assembly);

    builder.Services.AddInfrastructure(builder.Configuration);

    var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"]!;
    var jwtAudience = builder.Configuration["JwtSettings:Audience"]!;

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            };
        });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseBuildingBlocks();

    app.UseCors();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapRatingEndpoints();

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Rating service terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
