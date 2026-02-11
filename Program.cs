using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2147483648; // 2 GB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 2147483648; // 2 GB
});

var app = builder.Build();

// Služi statičke fajlove iz wwwroot
app.UseStaticFiles();

// Redirect na index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// API endpoint - test
app.MapGet("/api/test", () => new { status = "radi", timestamp = DateTime.Now });


byte[] key = [0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0xFE, 0xDC, 0xBA, 0x98, 0x76, 0x54, 0x32, 0x10];
byte[] iv = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];

CryptoHelperNamespace.CryptoHelper.EncryptionKey = key;
CryptoHelperNamespace.CryptoHelper.EncryptionIV = iv;

Console.WriteLine("\n=== KRIPTOGRAFSKI PARAMETRI ===");
Console.WriteLine($"Key: {BitConverter.ToString(key)}");
Console.WriteLine($"IV:  {BitConverter.ToString(iv)}");
Console.WriteLine("================================\n");

TestXXTEA.TestEncryptDecrypt();

var serverStatus = new System.Collections.Concurrent.ConcurrentBag<string>();
var fswStatus = new System.Collections.Concurrent.ConcurrentBag<string>(); // ← DODAJ OVO
FileWatching.FileSystemWatcherService? fswService = null; // ← DODAJ OVO

// Endpoint za dobijanje statusa
app.MapGet("/api/server-status", () =>
{
    var messages = serverStatus.ToList();
    serverStatus.Clear(); // Očisti posle čitanja

    if (messages.Count > 0)
        Console.WriteLine($"[API] Vraćam {messages.Count} poruka");

    return Results.Ok(new { messages });
});


// API endpoint za enkriptovanje
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

    // Enkriptuj
    byte[] encrypted = CryptoHelperNamespace.CryptoHelper.EncryptData(fileData, algorithm);

    // Računaj heš
    string hash = Hashing.TigerHash.ComputeHash(encrypted);

    return Results.Ok(new
    {
        success = true,
        filename = file.FileName,
        size = encrypted.Length,
        hash = hash,
        encryptedData = Convert.ToBase64String(encrypted)
    });
});

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

    // Dekriptuj
    byte[] decrypted = CryptoHelperNamespace.CryptoHelper.DecryptData(encryptedData, algorithm);

    return Results.Ok(new
    {
        success = true,
        filename = file.FileName,
        size = decrypted.Length,
        decryptedData = Convert.ToBase64String(decrypted)
    });
});
app.MapPost("/api/send", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var file = form.Files["file"];
        var algorithm = form["algorithm"].ToString(); // ← Ovo uzima algoritam iz forme
        var port = int.Parse(form["port"].ToString() ?? "5555");

        if (file == null)
            return Results.BadRequest(new { error = "Fajl nije poslat" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        byte[] fileData = ms.ToArray();

        // PRVO ENKRIPTUJ fajl
        byte[] encryptedData = CryptoHelperNamespace.CryptoHelper.EncryptData(fileData, algorithm);

        // Računaj heš ENKRIPTOVANIH podataka
        string fileHash = Hashing.TigerHash.ComputeHash(encryptedData);

        // Kreiraj metadata sa PRAVIM algoritmom
        string metadata = FileOps.MetadataHandler.CreateMetadata(
            file.FileName,
            encryptedData,
            algorithm, // ← Koristi algoritam iz forme
            "Tiger (SHA1)",
            fileHash
        );

        // Sačuvaj privremeno encrypted fajl i metadata
        string tempPath = Path.Combine(Path.GetTempPath(), file.FileName + ".enc");
        string metadataPath = tempPath + ".meta";

        await File.WriteAllBytesAsync(tempPath, encryptedData);
        await File.WriteAllTextAsync(metadataPath, metadata);

        // Pošalji preko TCP-a
        Network.TCPClient.SendFile(tempPath, "127.0.0.1", port);

        return Results.Ok(new { success = true, message = "Fajl enkriptovan i poslat preko TCP-a" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});



app.MapPost("/api/start-server", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var port = int.Parse(form["port"].ToString() ?? "5555");

        // Pokreni server sa callback-om koji dodaje poruke
        _ = Task.Run(async () =>
        {
            await Network.TCPServer.StartServerAsync(port, msg =>
            {
                serverStatus.Add(msg);
                Console.WriteLine($"[SERVER] Dodao poruku: {msg}"); // DEBUG
            });
        });

        return Results.Ok(new { success = true, message = $"Server pokrenut na portu {port}" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

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

        string hash = Hashing.TigerHash.ComputeHash(fileData);

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
app.MapPost("/api/start-fsw", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var targetPath = form["targetPath"].ToString() ?? "target";
        var algorithm = form["algorithm"].ToString() ?? "XXTEA-CBC";

        // Ako već radi, zaustavi ga prvo
        fswService?.StopWatching();

        // Kreiraj novi FSW servis
        fswService = new FileWatching.FileSystemWatcherService(algorithm, msg =>
        {
            fswStatus.Add(msg);
            Console.WriteLine($"[FSW] {msg}");
        });

        // Pokreni praćenje
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

app.MapGet("/api/fsw-status", () =>
{
    var messages = fswStatus.ToList();
    fswStatus.Clear();
    return Results.Ok(new { messages });
});





app.Run();
