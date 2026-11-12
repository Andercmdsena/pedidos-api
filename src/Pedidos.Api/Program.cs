using Amazon.Lambda.AspNetCoreServer.Hosting;
using System.Reflection;
using System.Text.Json.Serialization;
using Pedidos.Api.Repositories;
using Pedidos.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// En AWS Lambda (Function URL / API Gateway HTTP API) la app recibe los eventos HTTP;
// fuera de Lambda esta línea no hace nada y la app corre con Kestrel normalmente.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

if (Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME") is null)
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Plataforma de Gestión de Pedidos", Version = "v1" });
    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

builder.Services.AddSingleton<IPedidoRepository, InMemoryPedidoRepository>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.RoutePrefix = "swagger");

var version = builder.Configuration["APP_VERSION"] ?? "1.0";
var entorno = builder.Configuration["APP_ENV"] ?? app.Environment.EnvironmentName;

// Raíz y health check.
app.MapGet("/", () => Results.Ok(new
{
    servicio = "Plataforma de Gestión de Pedidos",
    version,
    entorno,
    docs = "/swagger"
}));
app.MapGet("/health", () => Results.Ok(new { status = "ok", version, entorno, fecha = DateTime.UtcNow }));

app.MapControllers();

app.Run();

// Necesario para pruebas de integración con WebApplicationFactory.
public partial class Program { }
