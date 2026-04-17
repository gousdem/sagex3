using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using SageX3WebApi.SageX3SoapClient;
using SageX3WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// -----------------------------------------------------------------------------
// Options
// -----------------------------------------------------------------------------
builder.Services
    .AddOptions<SageX3Options>()
    .Bind(builder.Configuration.GetSection(SageX3Options.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// -----------------------------------------------------------------------------
// HttpClient (typed) + Polly retry for transient failures
// -----------------------------------------------------------------------------
builder.Services
    .AddHttpClient<ISageX3Client, SageX3Client>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Sage X3 servers sometimes run self-signed certs in dev; leave strict here.
        // Override via ServerCertificateCustomValidationCallback only if you know what you're doing.
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    })
    .AddPolicyHandler(GetRetryPolicy());

// -----------------------------------------------------------------------------
// Application services
// -----------------------------------------------------------------------------
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IRemitToService, RemitToService>();

// -----------------------------------------------------------------------------
// MVC / Controllers / JSON
// -----------------------------------------------------------------------------
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Let our services return a uniform ApiResponse<T> rather than ASP.NET's ValidationProblemDetails.
        options.SuppressModelStateInvalidFilter = false;
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition =
            System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// -----------------------------------------------------------------------------
// CORS
// -----------------------------------------------------------------------------
const string CorsPolicy = "SageX3WebApiCors";
builder.Services.AddCors(options =>
{
    var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "*" };
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (allowed.Length == 1 && allowed[0] == "*")
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        else
            policy.WithOrigins(allowed).AllowAnyMethod().AllowAnyHeader();
    });
});

// -----------------------------------------------------------------------------
// Swagger / OpenAPI
// -----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sage X3 Web API",
        Version = "v1",
        Description = "REST wrapper over Sage X3 CAdxWebServiceXmlCC SOAP service."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// -----------------------------------------------------------------------------
// Problem details & health check
// -----------------------------------------------------------------------------
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

// =============================================================================
// Build and configure the pipeline
// =============================================================================
var app = builder.Build();

// Dev-friendly error page + Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Sage X3 Web API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseCors(CorsPolicy);
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();


// -----------------------------------------------------------------------------
// Polly: 3 retries with exponential backoff for transient HTTP errors + 408 + 5xx.
// -----------------------------------------------------------------------------
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(r => r.StatusCode == HttpStatusCode.RequestTimeout)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

// Expose the Program class for WebApplicationFactory in tests.
public partial class Program { }
