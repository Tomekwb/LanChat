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

// === RUNTIME ROOT (poza repo) ===
var runtimeRoot = @"C:\LanChat\runtime\server";

// === SQLite DB (poza repo): C:\LanChat\runtime\server\Data\lanchat.db ===
var dbPath = Path.Combine(runtimeRoot, "Data", "lanchat.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContext<LanChatDb>(opt =>
{
    opt.UseSqlite($"Data Source={dbPath}");
});

var app = builder.Build();

app.UseCors();

// === /updates -> C:\LanChat\runtime\server\Updates ===
var updatesPath = Path.Combine(runtimeRoot, "Updates");
Directory.CreateDirectory(updatesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(updatesPath),
    RequestPath = "/updates"
});

app.MapGet("/", () => "LanChatServer OK");

// UWAGA: klient łączy się do /chat (tak zostawiamy)
app.MapHub<ChatHub>("/chat");

// Tworzenie bazy / tabel przy starcie (bez migracji)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LanChatDb>();
    db.Database.EnsureCreated();
}

app.Run("http://0.0.0.0:5001");