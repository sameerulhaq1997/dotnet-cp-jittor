
using Jittor.App.DataServices;
using Jittor.App.Services;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//Register Services



builder.Services.AddSingleton<JittorDataServices>(provider =>
{
    string projectId = builder.Configuration.GetValue<string>("ProjectId");
    var repository = new FrameworkRepository("ConnectionStrings:CPConnectionString")
    {
        EnableAutoSelect = true,
    };
    return new JittorDataServices(repository, projectId);
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
