using System.Reflection;
using OpenTelemetry.Exporter;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

Action<ResourceBuilder> configureResource = r => r.AddService("MyWebApi");


// Add services to the container.
builder.Services.AddOpenTelemetryTracing(tBuilder => {
    tBuilder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyWebApi").AddTelemetrySdk())
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://localhost:4317");
        });
})
.AddOpenTelemetryMetrics(tBuilder => {
    tBuilder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("MyWebApi")
        .AddTelemetrySdk()
    )
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    .AddPrometheusExporter()
    .AddOtlpExporter(options => {
        options.Endpoint = new Uri("http://localhost:4317");
    });
}).AddHealthChecks();


// Logging
builder.Logging.ClearProviders();

builder.Logging.AddOpenTelemetry(options =>
{
    options.ConfigureResource(configureResource);
    options.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri("http://localhost:4317");
        });
});

builder.Services.Configure<OpenTelemetryLoggerOptions>(opt =>
{
    opt.IncludeScopes = true;
    opt.ParseStateValues = true;
    opt.IncludeFormattedMessage = true;
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapHealthChecks("/healthz");
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.MapControllers();

app.Run();
