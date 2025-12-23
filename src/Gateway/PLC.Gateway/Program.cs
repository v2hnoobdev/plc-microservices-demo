using Serilog;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Cau hinh Serilog
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.WithProperty("Service", "Gateway")
          .Enrich.WithMachineName()
          .Enrich.FromLogContext();
});

// Them YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        // Them TraceId vao request header
        builderContext.AddRequestTransform(transformContext =>
        {
            var traceId = transformContext.HttpContext.TraceIdentifier;
            transformContext.ProxyRequest.Headers.Add("X-Trace-Id", traceId);
            return ValueTask.CompletedTask;
        });
    });

// Them Health Checks
builder.Services.AddHealthChecks();

// Them CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware pipeline
app.UseSerilogRequestLogging();
app.UseCors();

// Map endpoints
app.MapReverseProxy();
app.MapHealthChecks("/health");

app.MapGet("/", () => new
{
    Service = "PLC.Gateway",
    Status = "Running",
    Version = "1.0.0",
    Timestamp = DateTime.UtcNow
});

app.Run();
