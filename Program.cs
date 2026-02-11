using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2147483648; // 2 GB
});

// Pronađi slobodan port
int port = 5000;
while (port < 5010)
{
    if (IsPortAvailable(port))
    {
        Console.WriteLine($"✅ Koristim port: {port}");
        break;
    }
    Console.WriteLine($"⚠️ Port {port} zauzet, probam sledeći...");
    port++;
}

if (port >= 5010)
{
    Console.WriteLine("❌ Nema slobodnih portova između 5000-5010!");
    return;
}

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2147483648; // 2 GB
    serverOptions.ListenLocalhost(port);
});

var app = builder.Build();

// Služi statičke fajlove
app.UseStaticFiles();

// Redirect na index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// API test
app.MapGet("/api/test", () => new { status = "radi", timestamp = DateTime.Now });

// Postavi globalni ključ i IV (16 bajtova za CBC!)
byte[] key = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10];
byte[] iv = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0, 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF]; // 16 bajtova!

CryptoHelperNamespace.CryptoHelper.EncryptionKey = key;
CryptoHelperNamespace.CryptoHelper.EncryptionIV = iv;

Console.WriteLine("\n=== KRIPTOGRAFSKI PARAMETRI ===");
Console.WriteLine($"Key: {BitConverter.ToString(key)}");
Console.WriteLine($"IV:  {BitConverter.ToString(iv)}");
Console.WriteLine("================================\n");

TestXXTEA.TestEncryptDecrypt();

var serverStatus = new System.Collections.Concurrent.ConcurrentBag<string>();
var fswStatus = new System.Collections.Concurrent.ConcurrentBag<string>();
FileWatching.FileSystemWatcherService? fswService = null;

// Server status
app.MapGet("/api/server-status", () =>
{
    var messages = serverStatus.ToList();
    serverStatus.Clear();
    if (messages.Count > 0)
        Console.WriteLine($"[API] Vraćam {messages.Count} poruka");
    return Results.Ok(new { messages });
});

// FSW status
app.MapGet("/api/fsw-status", () =>
{
    var messages = fswStatus.ToList();
    fswStatus.Clear();
    return Results.Ok(new { messages });
});

// Enkriptovanje
app.MapPost("/api/encrypt", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    var algorithm = form["algorithm"].ToString();

    if (file == null)
        return Results.BadRequest(new { error = "Fajl nije poslat" });

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    byte[] fileData = ms.ToArray();

    byte[] encrypted = CryptoHelperNamespace.CryptoHelper.EncryptData(fileData, algorithm);
    var tigerHash = new Hashing.TigerHash();
    string hash = tigerHash.ComputeHash(encrypted); 

    // ← DODAJ: Automatski sačuvaj u root
    CryptoHelperNamespace.CryptoHelper.SaveEncryptedFile(file.FileName, encrypted, algorithm);

    return Results.Ok(new
    {
        success = true,
        filename = file.FileName,
        size = encrypted.Length,
        hash = hash,
        saved = $"encrypted/{Path.GetFileName(file.FileName)}.enc"
    });
});


// Dekriptovanje
app.MapPost("/api/decrypt", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files["file"];
    var algorithm = form["algorithm"].ToString();

    if (file == null)
        return Results.BadRequest(new { error = "Fajl nije poslat" });

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    byte[] encryptedData = ms.ToArray();

    byte[] decrypted = CryptoHelperNamespace.CryptoHelper.DecryptData(encryptedData, algorithm);

    // ← ISPRAVKA: Proslijedi IME enkriptovanog fajla (sa .enc)
    CryptoHelperNamespace.CryptoHelper.SaveDecryptedFile(file.FileName, decrypted);

    return Results.Ok(new
    {
        success = true,
        originalName = Path.GetFileNameWithoutExtension(file.FileName),
        size = decrypted.Length,
        saved = $"decrypted/{Path.GetFileNameWithoutExtension(file.FileName)}"
    });
});

// Slanje fajla preko TCP
app.MapPost("/api/send", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files["file"];
        var algorithm = form["algorithm"].ToString();
        var ip = form["ip"].ToString() ?? "127.0.0.1";
        var port = int.Parse(form["port"].ToString() ?? "5555");

        if (file == null)
            return Results.BadRequest(new { error = "Fajl nije poslat" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        byte[] fileData = ms.ToArray();

        byte[] encryptedData = CryptoHelperNamespace.CryptoHelper.EncryptData(fileData, algorithm);
        var tigerHash = new Hashing.TigerHash();
        string fileHash = tigerHash.ComputeHash(encryptedData); 

        string metadata = FileOps.MetadataHandler.CreateMetadata(
            file.FileName,
            encryptedData,
            algorithm,
            "Tiger (SHA1)",
            fileHash
        );

        string tempPath = Path.Combine(Path.GetTempPath(), file.FileName + ".enc");
        string metadataPath = tempPath + ".meta";

        await File.WriteAllBytesAsync(tempPath, encryptedData);
        await File.WriteAllTextAsync(metadataPath, metadata);

        Network.TCPClient.SendFile(tempPath, ip, port);

        return Results.Ok(new { success = true, message = "Fajl enkriptovan i poslat preko TCP-a" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Pokretanje TCP servera
app.MapPost("/api/start-server", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var port = int.Parse(form["port"].ToString() ?? "5555");

        _ = Task.Run(async () =>
        {
            await Network.TCPServer.StartServerAsync(port, msg =>
            {
                serverStatus.Add(msg);
                Console.WriteLine($"[SERVER] Dodao poruku: {msg}");
            });
        });

        return Results.Ok(new { success = true, message = $"Server pokrenut na portu {port}" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// Hashovanje
app.MapPost("/api/hash", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files["file"];

        if (file == null)
            return Results.BadRequest(new { error = "Fajl nije poslat" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        byte[] fileData = ms.ToArray();

        var tigerHash = new Hashing.TigerHash();
        string hash = tigerHash.ComputeHash(fileData);

        return Results.Ok(new
        {
            success = true,
            filename = file.FileName,
            size = fileData.Length,
            hash = hash
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

// FSW
app.MapPost("/api/start-fsw", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var targetPath = form["targetPath"].ToString() ?? "target";
        var algorithm = form["algorithm"].ToString() ?? "XXTEA-CBC";

        fswService?.StopWatching();

        fswService = new FileWatching.FileSystemWatcherService(algorithm, msg =>
        {
            fswStatus.Add(msg);
            Console.WriteLine($"[FSW] {msg}");
        });

        await Task.Run(() => fswService.StartWatching(targetPath));

        return Results.Ok(new { success = true, message = $"FSW pokrenut za folder: {targetPath}" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

app.MapPost("/api/stop-fsw", () =>
{
    try
    {
        fswService?.StopWatching();
        fswService = null;
        return Results.Ok(new { success = true, message = "FSW zaustavljen" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

app.Run();

// Helper funkcija
static bool IsPortAvailable(int port)
{
    try
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch
    {
        return false;
    }
}
