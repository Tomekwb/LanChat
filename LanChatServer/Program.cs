using LanChatServer;
using LanChatServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials());
});

builder.Services.AddSignalR();

// SQLite (plik bazy w C:\LanChatServer\Data\lanchat.db)
var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "lanchat.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<LanChatDb>(opt =>
{
    opt.UseSqlite($"Data Source={dbPath}");
});

var app = builder.Build();

app.UseCors();

// === /updates -> C:\LanChatServer\Updates ===
var updatesPath = Path.Combine(app.Environment.ContentRootPath, "Updates");
Directory.CreateDirectory(updatesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(updatesPath),
    RequestPath = "/updates"
});

app.MapGet("/", () => "LanChatServer OK");

// UWAGA: u Ciebie klient łączy się do /chat (tak zostawiamy)
app.MapHub<ChatHub>("/chat");

// Tworzenie bazy / tabel przy starcie (bez migracji)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LanChatDb>();
    db.Database.EnsureCreated();
}

app.Run("http://0.0.0.0:5001");
