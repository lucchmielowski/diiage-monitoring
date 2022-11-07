using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenTelemetryTracing(builder => {
    builder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyWebApi").AddTelemetrySdk())
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://localhost:4317");
        });
})
.AddOpenTelemetryMetrics(builder => {
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
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
