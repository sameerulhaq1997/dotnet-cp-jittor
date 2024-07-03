
using Jittor.App.DataServices;
using Jittor.App.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//Register Services



builder.Services.AddSingleton<JittorDataServices>(provider =>
{
    var repository = new FrameworkRepository("ConnectionStrings:SCConnectionString")
    {
        EnableAutoSelect = true,
    };
    return new JittorDataServices(repository);
});
builder.Services.AddTransient<JittorApiService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
