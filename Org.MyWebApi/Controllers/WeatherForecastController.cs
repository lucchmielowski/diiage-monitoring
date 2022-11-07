using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Org.MyWebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;

    private static Counter<int> _requestCount;

    private static Counter<int> _errorCount;
    private static Histogram<float> _requestTimeHistogram;
    private static readonly Meter _baseMeter = new("Org.MyWebApi", "1.0");


    public WeatherForecastController(ILogger<WeatherForecastController> logger)
    {
        _logger = logger;
        _requestCount = _baseMeter.CreateCounter<int>("request_count");
        _errorCount   = _baseMeter.CreateCounter<int>("error_count");
        _requestTimeHistogram = _baseMeter.CreateHistogram<float>("request_time", "ms");
        _baseMeter.CreateObservableGauge("thread_count", () => ThreadPool.ThreadCount);


    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        var sw = Stopwatch.StartNew();
        _requestCount.Add(1);
        var retObj = Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
        sw.Stop();
        _requestTimeHistogram.Record(sw.ElapsedMilliseconds);
        _logger.LogInformation($"Request sent after {sw.ElapsedMilliseconds} ms.");
        return retObj;
    }
}
