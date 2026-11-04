using Amazon.DynamoDBv2;
using Amazon.SimpleNotificationService;
using Amazon.SQS;
using ExecucaoService.Application.Common;
using ExecucaoService.Application.UseCases;
using ExecucaoService.Domain.Interfaces;
using ExecucaoService.Infrastructure.Data;
using ExecucaoService.Infrastructure.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, config) =>
        config.ReadFrom.Configuration(context.Configuration)
              .Enrich.FromLogContext()
              .WriteTo.Console(new CompactJsonFormatter()));

    // 1. DynamoDB (banco próprio do Execução Service)
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddScoped<IExecucaoRepository, DynamoDbExecucaoRepository>();

    // 2. Mensageria
    builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
    builder.Services.AddAWSService<IAmazonSQS>();
    builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();
    builder.Services.AddHostedService<SqsEventConsumerBackgroundService>();

    // 3. Use Cases
    builder.Services.AddScoped<EnfileirarExecucaoUseCase>();
    builder.Services.AddScoped<IniciarExecucaoUseCase>();
    builder.Services.AddScoped<FinalizarExecucaoUseCase>();
    builder.Services.AddScoped<ConsultarExecucaoUseCase>();

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Execução Service - Oficina Wagyu (Fase 4)", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Bearer {token}\""
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
        });
    });

    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ChaveSecretaOficinaWagyu2026_MuitoLonga";
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    builder.Services.AddAuthentication(x =>
    {
        x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(x =>
    {
        x.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

    builder.Services.AddAuthorization();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "execucao-service", timestamp = DateTime.UtcNow }))
        .AllowAnonymous();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Execução Service terminou de forma inesperada durante a inicialização");
}
finally
{
    Log.CloseAndFlush();
}
