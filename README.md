# DIIAGE Monitoring tutorial

> La fiabilité est la fonctionalité la plus importante de n'importe quel système - Google's SRE Book


## The 3 pillars of Observability 

Les `logs`, `metrics` et `traces` sont souvent décrites comme les 3 piliers de l'observabilité. Bien que les avoir ne signifie
pas nécessairement avoir un système plus facile a observer, ce sont des outils qui, ils aident a construire
un système plus fiable mais aussi de pouvoir debugger ses   application.

## Logs

> Un `log` est un evenement immutable, qui a une datation (timestamp) et qui decrit un evenement qui s'est produit dans le temps.

Les logs peuvent prendre plusieurs formats:

1. `plaintext` qui est un format texte simple, c'est le format le plus commun
2. `structuré` qui le format qui est a prioriser et compte comme une bonne pratique. La plupart du temps ces logs sont au format `JSON`
3. `binaire` comme les `binlogs mysql`, ou les logs emits par des technologies comme `protobuf`

Les `logs` sont principalement a utiliser pour des cas de debugs. Ils permettent de gagner du contexter sur l'ensemble de nos requetes et pouvoir
mettre en lumiere des problématiques qui ne seraient pas remontées par les 2 autres outils.

### 👍
- Faciles a générer (il suffit de print un string ou un object JSON)
- La plupart des applications, frameworks, ... viennent avec des logs ou des librairies de logs
- Tres performant quand on a besoin d'information granulaire avec beaucoup de contexte (tant que le log est bien localise)

### 👎
- Trop de logging = perte de performance (en fonction des librairies)
- En fonction de la methode de distribution des logs, ils peuvent devenir inutiles
- En grand nombre les logs sont un challenge a ingerer pour les team ops (l'outils le plus utilise est la stack ELK: ElasticSearch + Kibana + Logstash)


## Metrics

> Les `metrics` sont une representation immutable d'une donnée mesurée sur un intervalle de temps. Elle permettent l'utilisation d'outils
mathematiques et de prediction pour en deduire le comportement d'un système.

Contrairement aux logs, les metrics sont optimisees pour pouvoir etre stockées, traitées et retrouvées rapidement, ce qui permet une plus grande retention.
C'est donc un sujet de choix pour pouvoir construire des `dashboards` qui donnent la performance, l'etat de l'application, etc.

### Structure d'une metric

La plupart des metrics sont composees des elements suivants:
- Un nom
- Un timestamp
- Des labels (clef-valeur)
- Une valeur

### Type de metrics

Les 3 types les plus connus sont:

`Counter`:
- Utilise pour representer une data qui s'incremente de maniere monotone (eg: Nombre de requetes, Creation d'un client, ...)
- Sa valeur ne devrait qu'augmenter ou etre reset a 0

`Gauge`:
- Utilise pour representer une valeur numerique qui peut arbitrairement monter ou descendre (eg: La temperature, La vitesse, ...)
- Peut etre utilise pour un `count` qui doit etre decroissant (eg: Nombre de requetes en parallele)

`Histogram`:
- Utilise pour representer un echantillon de valeurs distribue dans des `buckets` (eg: percentiles de latence des requetes)
- Un histogram emet en plus des data, la somme de toutes les valeurs observees, le compte d'evenements observes


A noter: certaines librairies implement d'autres types comme le `UpAndDownCounter`

### 👍

- Stockage et processing facilite par rapport aux logs
- Le cout n'augmente pas avec le traffic (contrairement aux logs)
- Plus malleable que les logs, permet d'utiliser des fonctions mathematiques (eg: Aggregation, correlation, etc)

### 👎

- Comme les logs, les metrics sont scopees a un system en particulier. Rendant le debug difficile dans le cas d'une application
distribuee


## Traces

> Une trace est une representation d'une serie d'evenements qui permettent d'encoder le flow complet d'une requete dans un systeme

Les traces sont une representation des `logs`, leur structure de donne se ressemble enormement. Une trace permet l'evaluation 
du path complet d'une requete dans un systeme ainsi que la structure de la requete. Pour fonctionner, **les differents services doivent
garder une forme de contexte entre les requetes**. Ce contexte est le plus souvent un `request ID` passe de requete en requete.

Exemple de system avec les traces associees:
![](./docs/system_to_trace.png)
![](./docs/traces.png)

### 👍

- Donne une visibilite complete du système

### 👎

- Challenging a mettre en place pour l'ensemble d'un infrastructure
- Si il manque des traces, cela peut rendre le debugging encore plus difficile (mieux vaut pas de traces que pas assez)
- Difficile d'instrumenter des applications que l'on ne controle pas (eg: DB, applications d'une autre equipe, ...)



## Workshop .NET + OpenTelemetry

De nombreuses librairies existent dans tous les languages afin de pouvoir utiliser les traces / logs / metrics.
Pour ce workshop nous allons utiliser un outil qui s'appelle [OpenTelemetry](https://opentelemetry.io/).

> OpenTelemetry is a collection of tools, APIs, and SDKs. Use it to instrument, generate, collect, and export telemetry data (metrics, logs, and traces) to help you analyze your software’s performance and behavior.

## Ce que l'on va construire

![](./docs/archi.png)

Pour l'application, elle tournera en local sur notre machine

### Creer l'application

Pour ce workshop nous allons nous baser sur l'application par defaut cree par .NET

```shell
dotnet new webapi -n Org.MyWebApi --no-https 
```

### Lancer le collector + zipkin + prometheus

Afin de pouvoir recuperer nos data, ils faut aussi que l'on monte l'infrastructure de monitoring :

```bash
cd infra && docker-compose up -d
```

### Ajouter des traces

```csharp
// In Program.cs
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

..
var builder = WebApplication.CreateBuilder(args);

// Add OpenTelemetryTracing to the different Services
builder.Services.AddOpenTelemetryTracing(builder => {  
    builder
        // Add automatic instrumation for HTTP Request (AspNetCore)
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        // Add tags for the service so it's known as "MyWebApi"
        .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyWebApi").AddTelemetrySdk())
        // Add the exporter to OpenTelemetry collector
        .AddOtlpExporter(options => {
            options.Endpoint = new Uri("http://localhost:4317");
        });
});
```

Et Tadaa vous avez les bases des traces sur votre service 🎉.

Vous pouvez tester en allant sur `http://localhost:<port>/weatherforecast`

Puis en allant sur [Zipkin](http://localhost:9411).

![](./docs/zipkin.png)

### Ajouter des metrics

```csharp
// Program.cs
using OpenTelemetry.Metrics;
...
builder.AddOpenTelemetryMetrics(builder => {
    // Register our service as MyWebApi
    builder.SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("MyWebApi")
        .AddTelemetrySdk()
    )
    // Default instrumentation
    .AddAspNetCoreInstrumentation()
    .AddRuntimeInstrumentation()
    // Adds a Promehteus exporter (only used for debuging) 
    .AddPrometheusExporter()
    // Adds the OpenTelemetry collector exporter
    .AddOtlpExporter(options => {
        options.Endpoint = new Uri("http://localhost:4317");
    });
});

...

var app = builder.Build();
...
app.UseOpenTelemetryPrometheusScrapingEndpoint();
```

Une fois l'application modifiee et redemaree, il faut refaire une requete a l'API
en allant a `http://localhost:<port>/weatherforecast`

Cela devrait creer des metrics, visibles au niveau du service via l'exporter Prometheus si vous allez au `http://localhost:<port>/metrics` 
(qui n'est pas utilise pour l'export de metrics) 

mais aussi dans Prometheus directement en allant a `http://localhost:9090/` et en faisant une query via PromQL dans l'onglet `Graph`:

```promql
http_server_duration_bucket{http_status_code="200",http_target="WeatherForecast"}
```

![](./docs/prometheus_exporter.png)
![](./docs/prometheus_app.png)

### Ajouter des metrics "custom"

En utilisant `AddAspNetCoreInstrumentation()` et `AddRuntimeInstrumentation()` OpenTelemetry nous fournit
automatiquement des metrics pour les requetes HTTP de notre service. Mais cela n'est pas toujours suffisant, vous pouvez ajouter des metrics
par vous meme dans votre application de la maniere suivante:

```csharp
// Controllers/WeatherForecastController.cs
using System.Diagnostics;
using System.Diagnostics.Metrics;

...

public class WeatherForecastController : ControllerBase
{
   ...
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
        // Add +1 to the number of requests
        _requestCount.Add(1);
        
        ...
        
        sw.Stop();
        // Add the execution time of the request to the Histogram
        _requestTimeHistogram.Record(sw.ElapsedMilliseconds);
        // Adds a log when the request's done
        _logger.LogInformation($"Request sent after {sw.ElapsedMilliseconds} ms.");
    }
    
    ...
}
```

Vous pourrez ensuite les recuperer dans votre outil de monitoring directement via 
des requetes sur le nom de la metric (eg: `error_count`, `request_count`, ...)


## Bonus: Ajouter un healthcheck

Comme vous avez pu le voir, Kubernetes necessite d'avoir un healthcheck pour pouvoir utiliser les `ReadinessProbes` et `LivenessProbes`.

Vous pouvez en ajouter un endpoint de check simplement a votre application en faisant:

```csharp
// Program.cs
...
builder.AddHealthChecks();
...
var app = builder.Build();
...
app.MapHealthChecks("/healthz");
```

