using System.Text.Json;
using LanChatServer;
using LanChatServer.Data;
using LanChatServer.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 500L * 1024 * 1024; // 500 MB
});

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .SetIsOriginAllowed(_ => true)
        .AllowCredentials());
});

builder.Services.AddSignalR();

// zwiększamy limit uploadu (domyślnie jest mały)
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 500L * 1024 * 1024; // 500 MB
});

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

// === /files -> C:\LanChat\runtime\server\Files ===
var filesPath = Path.Combine(runtimeRoot, "Files");
Directory.CreateDirectory(filesPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesPath),
    RequestPath = "/files"
});

app.MapGet("/", () => "LanChatServer OK");

// upload pliku + zapis wiadomości + powiadomienie odbiorcy
app.MapPost("/files/upload", async (HttpRequest req, LanChatDb db, IHubContext<ChatHub> hub) =>
{
    var form = await req.ReadFormAsync();

    var fromUser = (form["fromUser"].ToString() ?? "").Trim();
    var toUser = (form["toUser"].ToString() ?? "").Trim();
    var file = form.Files.GetFile("file");

    if (string.IsNullOrWhiteSpace(fromUser) || string.IsNullOrWhiteSpace(toUser) || file is null)
        return Results.BadRequest("Missing fromUser/toUser/file");

    if (file.Length <= 0)
        return Results.BadRequest("Empty file");

    // 200MB limit po stronie aplikacji (obok FormOptions)
    const long maxBytes = 200L * 1024 * 1024;
    if (file.Length > maxBytes)
        return Results.BadRequest("File too large");

    // upewnij się, że user istnieje w tabeli Users (offline kontakt)
    var exists = await db.Users.AnyAsync(u => u.User == toUser);
    if (!exists)
    {
        db.Users.Add(new ChatUser
        {
            User = toUser,
            Machine = "",
            IsOnline = false,
            LastSeenUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // zapis pliku na dysk
    var originalName = Path.GetFileName(file.FileName);
    var safeName = string.Concat(originalName.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
    if (string.IsNullOrWhiteSpace(safeName)) safeName = "file.bin";

    var storedName = $"{Guid.NewGuid():N}_{safeName}";
    var fullPath = Path.Combine(filesPath, storedName);

    await using (var fs = File.Create(fullPath))
    await using (var stream = file.OpenReadStream())
    {
        await stream.CopyToAsync(fs);
    }

    var baseUrl = $"{req.Scheme}://{req.Host}";
    var fileUrl = $"{baseUrl}/files/{Uri.EscapeDataString(storedName)}";

    // payload wiadomości typu plik (bez zmian w DB schema)
    var body = $"LCFILE|{originalName}|{file.Length}|{fileUrl}";

    var recipientOnline = await db.Users.AnyAsync(u => u.User == toUser && u.IsOnline);

    db.Messages.Add(new ChatMessage
    {
        FromUser = fromUser,
        ToUser = toUser,
        Body = body,
        SentAtUtc = DateTime.UtcNow,
        DeliveredAtUtc = recipientOnline ? DateTime.UtcNow : null
    });

    await db.SaveChangesAsync();

    // SignalR: wysyłamy do grupy usera (Group = nazwa użytkownika)
    if (recipientOnline)
        await hub.Clients.Group(toUser).SendAsync("PrivateMessage", fromUser, body);

    return Results.Json(new
    {
        ok = true,
        body,
        fileUrl,
        fileName = originalName,
        size = file.Length
    });
});

// UWAGA: klient łączy się do /chat (tak zostawiamy)
app.MapHub<ChatHub>("/chat");

// Tworzenie bazy / tabel przy starcie (bez migracji)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LanChatDb>();
    db.Database.EnsureCreated();
}

app.Run("http://0.0.0.0:5001");