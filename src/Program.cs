using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

if(!Directory.Exists("/data"))
{
    Console.Error.WriteLine("it appears you did not mount a data volume, please beware your data may be lost if you ever delete this container");
    Directory.CreateDirectory("/data");
}
if(!Directory.Exists("/data/torrents"))
{
    Directory.CreateDirectory("/data/torrents");
}

Console.WriteLine("Applying migrations");
await ReelGrab.Database.Db.ApplyMigrationsAsync();
Console.WriteLine("Migrations complete");

Console.WriteLine("Applying MediaIndex configuration");
await ReelGrab.Configuration.MediaIndex.instance.Apply();
Console.WriteLine("MediaIndex configuration completed");

Console.WriteLine("Applying StorageGateway configuration");
await ReelGrab.Configuration.StorageGateway.instance.Apply();
Console.WriteLine("StorageGateway configuration completed");

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

builder.Services.AddHostedService<ReelGrab.Core.Background.Torrents.SyncRequestedTorrents>();
builder.Services.AddHostedService<ReelGrab.Core.Background.Movies.SyncRequestedTorrentFiles>();
builder.Services.AddHostedService<ReelGrab.Core.Background.Movies.DownloadCompletedTorrentFiles>();
builder.Services.AddHostedService<ReelGrab.Core.Background.Series.SyncRequestedTorrentFiles>();
builder.Services.AddHostedService<ReelGrab.Core.Background.Series.DownloadCompletedTorrentFiles>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseCors("Everything");
}

app.MapWhen(
    context => !context.Request.Path.StartsWithSegments("/api") && !Path.HasExtension(context.Request.Path),
    appBuilder =>
    {
        appBuilder.Use(async (context, next) =>
        {
            context.Request.Path = "/index.html";
            await next();
        });
        appBuilder.UseStaticFiles();
    }
);

app.MapControllers();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://*:5242");
    app.UseSwagger();
    app.UseSwaggerUI();
}

if(app.Environment.IsProduction())
{
    app.Urls.Add("http://*:80");
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.Run();
