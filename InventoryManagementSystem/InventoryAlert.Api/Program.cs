using InventoryAlert.Api;
using InventoryAlert.Api.Application.Interfaces;
using InventoryAlert.Api.Application.Services;
using InventoryAlert.Api.Infrastructure;
using InventoryAlert.Api.Infrastructure.External;
using InventoryAlert.Api.Infrastructure.External.Interfaces;
using InventoryAlert.Api.Infrastructure.Persistence.Interfaces;
using InventoryAlert.Api.Infrastructure.Persistence.Repositories;
using InventoryAlert.Api.Infrastructure.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RestSharp;

var builder = WebApplication.CreateBuilder(args);

// TODO: Review and suggest enhance the program.cs design, consider to use extension method to organize the code and make it more readable and maintainable
var configuration = builder.Configuration.Get<AppSettings>() ?? throw new Exception("Appsetting is missing!");
builder.Services.AddSingleton(configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "🚀 Pragmatic Inventory Alert API",
        Version = "v1",
        Description = "Real-time stock monitoring and significant price loss detection system integrated with Finnhub API.",
        Contact = new OpenApiContact
        {
            Name = "OJT Training Team",
            Email = "dev@ojt-training.local"
        }
    });
});

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.Database.DefaultConnection));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductServices>();
builder.Services.AddHostedService<FinnhubSyncWorker>();

// TODO: MOVE THE NAME THE STATIC FILE TO CONSTANT
builder.Services.AddHttpClient("Finnhub", client =>
{
    client.BaseAddress = new Uri(configuration.Finnhub.ApiBaseUrl);
});

// Configure IFinnhubClient to pull from the factory
builder.Services.AddScoped<IFinnhubClient>(sp => 
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = factory.CreateClient("Finnhub"); // Reuse the managed client
    
    // Create RestClient using that HttpClient and inject it
    return new FinnhubClient(new RestClient(httpClient), configuration);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate(); 
} 

// Configure the HTTP request pipeline. 
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Docker"))
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "V1 Docs");
        options.RoutePrefix = "swagger"; // Root access
        options.DocumentTitle = "Pragmatic Inventory API Docs";
    });
}

app.UseAuthorization();

app.MapControllers();

app.Run();

