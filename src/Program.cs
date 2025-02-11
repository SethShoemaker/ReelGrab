using System.Text.Json.Serialization;
using ReelGrab.Core;
using ReelGrab.Core.Processing;
using ReelGrab.Database;
using ReelGrab.MediaIndexes;
using ReelGrab.Storage;
using ReelGrab.TorrentIndexes;
using ReelGrab.Web.Routers;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Applying migrations");
await Db.ApplyMigrationsAsync();
Console.WriteLine("Migrations complete");

Console.WriteLine("Applying MediaIndex configuration");
await MediaIndexConfig.instance.ApplyMediaIndexConfigAsync();
Console.WriteLine("MediaIndex configuration completed");

Console.WriteLine("Applying StorageGateway configuration");
await StorageGatewayConfig.instance.ApplyStorageGatewayConfigAsync();
Console.WriteLine("StorageGateway configuration completed");

Console.WriteLine("Initializing TorrentIndex");
await TorrentIndex.instance.InitializeConfigurationAsync();
Console.WriteLine("Initialized TorrentIndex");

var syncTorrentFiles = new SyncTorrentFiles();
var uploadCompleted = new UploadCompleted();
using var cts = new CancellationTokenSource();
syncTorrentFiles.StartAsync(cts.Token);
uploadCompleted.StartAsync(cts.Token);
Console.CancelKeyPress += (sender, eventArgs) =>
{
    cts.Cancel();
    eventArgs.Cancel = true; // Prevent immediate termination
    syncTorrentFiles.StopAsync(cts.Token);
    uploadCompleted.StopAsync(cts.Token);
};

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

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseCors("Everything");
}
var mediaIndexRouter = new MediaIndexRouter();
mediaIndexRouter.Route(app);
var storageGatewayRouter = new StorageGatewayRouter();
storageGatewayRouter.Route(app);
var torrentIndexRouter = new TorrentIndexRouter();
torrentIndexRouter.Route(app);
var wantedMediaRouter = new MediaWantedRouter();
wantedMediaRouter.Route(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://*:5242");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
