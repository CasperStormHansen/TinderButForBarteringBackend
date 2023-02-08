using Microsoft.EntityFrameworkCore;
using TinderButForBarteringBackend;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BarterDatabase>(opt => opt.UseSqlite("Data Source=data/BarterDatabase.db"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.EnableDetailedErrors = true;
    hubOptions.MaximumReceiveMessageSize = 10000000; // TODO: What should this value be?
});
var app = builder.Build();

app.MapHub<ComHub>("/comhub");

app.MapGet("/images/{filename}", async (string filename, HttpResponse response) =>
{
    var path = Path.Combine(@"Data/Images/", filename);
    var fileBytes = await File.ReadAllBytesAsync(path);
    response.ContentType = "image/jpeg";
    await response.Body.WriteAsync(fileBytes);
});

app.Run();