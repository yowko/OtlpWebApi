using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;



var builder = WebApplication.CreateBuilder(args);
ActivitySource sSource = new ActivitySource(builder.Environment.ApplicationName);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient("local",app =>
    {
        app.BaseAddress = new Uri("http://localhost:5118/");
    }
);

var tracingOtlpEndpoint = builder.Configuration.GetValue("Otlp:Endpoint", defaultValue: "http://localhost:4317")!;
var otel = builder.Services.AddOpenTelemetry();

// Configure OpenTelemetry Resources with the application name
otel.ConfigureResource(resource => resource
    .AddService(serviceName: builder.Environment.ApplicationName));
otel.WithTracing(tracing =>
{
    tracing.AddAspNetCoreInstrumentation();
    tracing.AddHttpClientInstrumentation();
    tracing.AddSource(sSource.Name );
    if (tracingOtlpEndpoint != null)
    {
        tracing.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(tracingOtlpEndpoint); 
        });
    }
    // else
    // {
        tracing.AddConsoleExporter();
    //}
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/getweatherforecast", async () =>
    {
        using Activity activity = sSource.StartActivity("call GetWeatherAsync");
        return await GetWeatherAsync();
    })
    .WithName("GetWeatherForecast");

app.Run();

async Task<HttpResponseMessage> GetWeatherAsync()
{
    using Activity activity = sSource.StartActivity("exec GetWeatherAsync");
    activity?.SetTag("yowko", "tag OK");

    var httpclient = app.Services.GetService<IHttpClientFactory>().CreateClient("local");
    return await httpclient.GetAsync("/weatherforecast");
}