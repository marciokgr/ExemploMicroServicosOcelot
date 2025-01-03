using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// health check
builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

// Ocelot
builder.Services.AddOcelot(builder.Configuration);
builder.Services.AddSwaggerForOcelot(builder.Configuration); //adiciona o swagger para o ocelot

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        });
});

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    //Serilog.
    loggerConfiguration
        .MinimumLevel.Information()
        .Enrich.WithProperty("ApplicationContext", "API.Gateway")
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .ReadFrom.Configuration(context.Configuration);
});

// Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json");

WebApplication app = builder.Build();

//health check
app.MapHealthChecks("/hc", new HealthCheckOptions()
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
});

//swagger ocelot, APIs filhas
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseEndpoints(_ => { });

app.UseCors("AllowAll"); //CORS

await app.UseOcelot(); //API Gateway Ocelot
await app.RunAsync();
