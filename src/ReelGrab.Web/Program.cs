using System.Text.Json.Serialization;
using ReelGrab.Core;
using ReelGrab.Web.Routers;

var builder = WebApplication.CreateBuilder(args);

Console.WriteLine("Applying migrations");
await ReelGrab.Core.Application.instance.ApplyMigrationsAsync();
Console.WriteLine("Migrations complete");

Console.WriteLine("Applying MediaIndex configuration");
await ReelGrab.Core.Application.instance.ApplyMediaIndexConfigAsync();
Console.WriteLine("MediaIndex configuration completed");

Console.WriteLine("Applying StorageGateway configuration");
await ReelGrab.Core.Application.instance.ApplyStorageGatewayConfigAsync();
Console.WriteLine("StorageGateway configuration completed");

Console.WriteLine("Applying StorageGateway configuration");
await ReelGrab.Core.Application.instance.ApplyTorrentIndexConfigAsync();
Console.WriteLine("StorageGateway configuration completed");

Console.WriteLine("Applying TorrenClient configuration");
await ReelGrab.Core.Application.instance.ApplyTorrentClientConfigAsync();
Console.WriteLine("TorrenClient configuration completed");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (sender, eventArgs) =>
{
    cts.Cancel();
    eventArgs.Cancel = true; // Prevent immediate termination
};
Application.instance.ProcessWantedMediaBackgroundAsync(cts.Token);

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
var torrentClientRouter = new TorrentClientRouter();
torrentClientRouter.Route(app);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.Urls.Add("http://*:5242");
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Run();
