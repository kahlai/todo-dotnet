using Microsoft.EntityFrameworkCore;
using TodoApi.Models;

using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

IConfigurationRoot configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();
string connectionString = configuration.GetConnectionString("DefaultConnection");
//"Server=mssql;Database=todo;User Id=sa; Password=Zxcvbnm<>1;TrustServerCertificate=True;MultiSubnetFailover=True;Encrypt=false";
//configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<TodoContext>(options =>
              options.UseSqlServer(connectionString, providerOptions => providerOptions.EnableRetryOnFailure()));
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string serviceName = "todo-development";
string otlpTraceEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_TRACE_ENDPOINT") ?? builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!;
Console.Out.WriteLine("otlpTraceEndpoint: " + otlpTraceEndpoint);
builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService(serviceName))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.EnrichWithIDbCommand = (activity, command) =>
                {
                    var stateDisplayName = $"{command.CommandType} main";
                    activity.DisplayName = stateDisplayName;
                    activity.SetTag("db.name", stateDisplayName);
                };
            })
          .AddConsoleExporter()
          .AddOtlpExporter(
            options =>
            {
                //options.Endpoint = new Uri("http://localhost:4317/v1/traces");
                options.Endpoint = new Uri(otlpTraceEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
            }
          ))
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter()
          .AddOtlpExporter());

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// DefaultFilesOptions options = new DefaultFilesOptions();
// options.DefaultFileNames.Clear();
// options.DefaultFileNames.Add("index.html");
//app.UseDefaultFiles(options);
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
