
using Jittor.App;
using Jittor.App.DataServices;
using Jittor.App.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using PetaPoco;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var configurationBuilder = new ConfigurationBuilder();
string path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
configurationBuilder.AddJsonFile(path, false);

var connectionStrings = new Dictionary<string, string>
        {
            { "SCConnectionString", configurationBuilder.Build().GetSection("ConnectionStrings:SCConnectionString").Value ?? "" },
            { "CPConnectionString", configurationBuilder.Build().GetSection("ConnectionStrings:CPConnectionString").Value ?? "" },
            { "ArgaamConnectionString", configurationBuilder.Build().GetSection("ConnectionStrings:ArgaamConnectionString").Value ?? "" }
        };
builder.Services.AddJittorApp(connectionStrings,10);

//Register Services
builder.Services.AddSingleton<JittorDataServices>(provider =>
{
    string projectId = builder.Configuration.GetValue<string>("ProjectId");
    var dbPoolManager = provider.GetRequiredService<DatabasePoolManager>();
    //var repository = dbPoolManager.GetDatabase();  // Retrieve from the pool
    var repository = new FrameworkRepository("ConnectionStrings:CPConnectionString")
    {
        EnableAutoSelect = true,
    };
    var secondaryRepository = new FrameworkRepository("ConnectionStrings:ArgaamConnectionString")
    {
        EnableAutoSelect = true,
    };
    return new JittorDataServices(repository, projectId, dbPoolManager,secondaryRepository);
});
builder.Services.AddTransient<JittorApiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("SwaggerApi", new OpenApiInfo
    {
        Version = "v1",
        Title = "Swagger API",
        Description = "A simple example ASP.NET Core Web API"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "CORS_Everyone", builder =>
    {
        builder.WithOrigins("*")
         .WithHeaders(HeaderNames.ContentType, "x-custom-header")
         .WithMethods("POST", "GET", "OPTIONS")
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});


var app = builder.Build();
app.Services.GetService<DatabasePoolManager>();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/SwaggerApi/swagger.json", "Swagger API");
});
app.UseCors("CORS_Everyone");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
