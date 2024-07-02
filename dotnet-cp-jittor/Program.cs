
using Jittor.App.DataServices;
using Jittor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

//Register Services



builder.Services.AddTransient<JittorDataServices>(provider =>
{
    var repository = new FrameworkRepository("ConnectionStrings:SCConnectionString")
    {
        EnableAutoSelect = true,
    };
    return new JittorDataServices(repository);
});

//builder.Services.AddTransient<JittorDataServices>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
