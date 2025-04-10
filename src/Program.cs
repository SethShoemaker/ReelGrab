using System.Text.Json.Serialization;
using ReelGrab.Core.Background.Movies;
using ReelGrab.Core.Background.Series;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Applying migrations");
await ReelGrab.Database.Db.ApplyMigrationsAsync();
Console.WriteLine("Migrations complete");

Console.WriteLine("Applying MediaIndex configuration");
await ReelGrab.Configuration.MediaIndex.instance.Apply();
Console.WriteLine("MediaIndex configuration completed");

Console.WriteLine("Applying StorageGateway configuration");
await ReelGrab.Configuration.StorageGateway.instance.Apply();
Console.WriteLine("StorageGateway configuration completed");

Console.WriteLine("Initializing TorrentIndex");
await ReelGrab.Configuration.TorrentIndex.instance.Apply();
Console.WriteLine("Initialized TorrentIndex");

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Everything", builder =>
    {
        builder.AllowAnyHeader();
        builder.AllowAnyOrigin();
        builder.AllowAnyMethod();
    });
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddControllers();

builder.Services.AddHostedService<AddMovieTorrents>();
builder.Services.AddHostedService<ProcessCompletedMovies>();
builder.Services.AddHostedService<AddSeriesTorrents>();
builder.Services.AddHostedService<ProcessCompletedSeriesEpisodes>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseCors("Everything");
}
app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://*:5242");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
