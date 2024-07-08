
using Jittor.App.DataServices;
using Jittor.App.Services;
using Microsoft.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//Register Services



builder.Services.AddSingleton<JittorDataServices>(provider =>
{
    var repository = new FrameworkRepository("ConnectionStrings:CPConnectionString")
    {
        EnableAutoSelect = true,
    };
    return new JittorDataServices(repository);
});
builder.Services.AddTransient<JittorApiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("CORS_Everyone");
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
