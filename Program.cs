using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Sockets;
using CryptoHelperNamespace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2147483648; 
});


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
    serverOptions.Limits.MaxRequestBodySize = 2147483648; 
    serverOptions.ListenLocalhost(port);
});

var app = builder.Build();


app.UseStaticFiles();


app.MapGet("/", () => Results.Redirect("/index.html"));


app.MapGet("/api/test", () => new { status = "radi", timestamp = DateTime.Now });






byte[] key;
const string KEY_FILE = "shared.key";

if (File.Exists(KEY_FILE))
{
    key = File.ReadAllBytes(KEY_FILE);
    Console.WriteLine($"✅ Učitan postojeći ključ iz {KEY_FILE}");
}
else
{
    key = CryptoHelperNamespace.KeyManager.GenerateXXTEAKey();
    CryptoHelperNamespace.KeyManager.SaveKey(key, KEY_FILE);
    Console.WriteLine($"🔑 Generisan novi ključ i sačuvan u {KEY_FILE}");
}

CryptoHelperNamespace.CryptoHelper.EncryptionKey = key;


Console.WriteLine("\n=== KRIPTOGRAFSKI PARAMETRI ===");
Console.WriteLine($"Key: {BitConverter.ToString(key)}");
Console.WriteLine($"IV:  Generiše se automatski pri svakom enkriptovanju (CBC)");
Console.WriteLine("================================\n");


TestXXTEA.TestEncryptDecrypt();

var serverStatus = new System.Collections.Concurrent.ConcurrentBag<string>();
var fswStatus = new System.Collections.Concurrent.ConcurrentBag<string>();
FileWatching.FileSystemWatcherService? fswService = null;


app.MapGet("/api/server-status", () =>
{
    var messages = serverStatus.ToList();
    serverStatus.Clear();
    if (messages.Count > 0)
        Console.WriteLine($"[API] Vraćam {messages.Count} poruka");
    return Results.Ok(new { messages });
});


app.MapGet("/api/fsw-status", () =>
{
    var messages = fswStatus.ToList();
    fswStatus.Clear();
    return Results.Ok(new { messages });
});


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

app.MapPost("/api/debug-crypt", async (HttpRequest request) =>
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

        Console.WriteLine($"\n🔍 === DEBUG .crypt FAJL ===");
        Console.WriteLine($"Naziv: {file.FileName}");
        Console.WriteLine($"Ukupna veličina: {fileData.Length} bajtova");

        
        byte[] iv = new byte[16];
        Array.Copy(fileData, 0, iv, 0, 16);
        Console.WriteLine($"\nIV (0-15): {BitConverter.ToString(iv)}");

        
        int remainingSize = fileData.Length - 16;
        Console.WriteLine($"Ostatak: {remainingSize} bajtova");
        Console.WriteLine($"Deljiv sa 16: {remainingSize % 16 == 0} ({remainingSize % 16} remainder)");

        
        byte[] next64 = new byte[Math.Min(64, remainingSize)];
        Array.Copy(fileData, 16, next64, 0, next64.Length);
        Console.WriteLine($"\nBajtovi 16-79 (hex):\n{BitConverter.ToString(next64)}");

        
        string textAttempt = System.Text.Encoding.UTF8.GetString(next64);
        Console.WriteLine($"\nBajtovi 16-79 (text): {textAttempt.Replace("\n", "\\n").Replace("\r", "\\r")}");

        
        byte[] lastBlock = new byte[16];
        if (fileData.Length >= 16)
        {
            int lastBlockStart = ((fileData.Length - 16) / 16) * 16;
            if (lastBlockStart >= 16)
            {
                Array.Copy(fileData, lastBlockStart, lastBlock, 0, 16);
                Console.WriteLine($"\nPoslednji 16-byte blok ({lastBlockStart}-{lastBlockStart + 15}):");
                Console.WriteLine($"Hex: {BitConverter.ToString(lastBlock)}");
                Console.WriteLine($"Decimal: {string.Join(", ", lastBlock)}");
            }
        }

        
        var tigerHash = new Hashing.TigerHash();
        string hash = tigerHash.ComputeHash(fileData);
        Console.WriteLine($"\nTiger hash: {hash.Substring(0, 32)}...");

        Console.WriteLine($"=========================\n");

        return Results.Ok(new
        {
            success = true,
            size = fileData.Length,
            ivHex = BitConverter.ToString(iv).Replace("-", ""),
            remainingSize = remainingSize,
            isDivisibleBy16 = remainingSize % 16 == 0,
            remainder = remainingSize % 16,
            next64Hex = BitConverter.ToString(next64).Replace("-", ""),
            next64Text = textAttempt,
            hash = hash.Substring(0, 32)
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
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
    byte[] fileData = ms.ToArray();

    Console.WriteLine($"\n🔍 DEBUG DECRYPT:");
    Console.WriteLine($"   Fajl: {file.FileName}");
    Console.WriteLine($"   Veličina: {fileData.Length} bajtova");
    Console.WriteLine($"   Algoritam: {algorithm}");

    try
    {
        
        var (iv, encryptedData, metadata) = FileOps.FileFormatParser.ParseCryptFile(fileData);

        Console.WriteLine($"   IV: {BitConverter.ToString(iv)}");
        Console.WriteLine($"   Encrypted size: {encryptedData.Length}");
        
        if (metadata != null)
        {
            Console.WriteLine($"   Metadata pronađena: {metadata.FileName}, {metadata.Algorithm}");
            algorithm = metadata.Algorithm; 
        }

        
        var xxtea = new CryptoHelperNamespace.Ciphers.XXTEA(CryptoHelperNamespace.CryptoHelper.EncryptionKey);
        var cbc = new CryptoHelperNamespace.Ciphers.CBC(xxtea, iv); 
        
        
        byte[] fullData = new byte[16 + encryptedData.Length];
        Array.Copy(iv, 0, fullData, 0, 16);
        Array.Copy(encryptedData, 0, fullData, 16, encryptedData.Length);
        
        byte[] decrypted = cbc.Decrypt(fullData);

        Console.WriteLine($"   ✅ Dekriptovano: {decrypted.Length} bajtova");

        string originalName = metadata?.FileName ?? Path.GetFileNameWithoutExtension(file.FileName);
        CryptoHelperNamespace.CryptoHelper.SaveDecryptedFile(originalName, decrypted);

        return Results.Ok(new
        {
            success = true,
            originalName = originalName,
            size = decrypted.Length,
            saved = $"decrypted/{originalName}"
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"   ❌ GREŠKA: {ex.Message}");
        Console.WriteLine($"   Stack: {ex.StackTrace}");
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});




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

        string metadata = FileOps.MetadataHandler.CreateCompatibleMetadata(
        file.FileName,
        encryptedData,
        algorithm,
        "Tiger-Hash",
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


app.MapPost("/api/generate-key", () =>
{
    try
    {
        byte[] key = CryptoHelperNamespace.KeyManager.GenerateXXTEAKey();
        CryptoHelperNamespace.KeyManager.SaveKey(key);
        CryptoHelperNamespace.KeyManager.ApplyKey(key);

        return Results.Ok(new
        {
            success = true,
            message = "Ključ generisan i sačuvan u shared.key!",
            keyHex = BitConverter.ToString(key).Replace("-", "")
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});



app.MapGet("/api/download-key", () =>
{
    try
    {
        if (!File.Exists("shared.key"))
            return Results.NotFound(new { error = "Ključ nije generisan!" });

        byte[] keyData = File.ReadAllBytes("shared.key");
        return Results.File(keyData, "application/octet-stream", "shared.key");
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});

app.MapPost("/api/upload-key", async (HttpRequest request) =>
{
    try
    {
        var form = await request.ReadFormAsync();
        var keyFile = form.Files["keyFile"];

        if (keyFile == null)
            return Results.BadRequest(new { error = "Ključ fajl nije poslat!" });

        using var ms = new MemoryStream();
        await keyFile.CopyToAsync(ms);
        byte[] key = ms.ToArray();

        if (key.Length != 16)
            return Results.BadRequest(new { error = $"Ključ mora biti 16 bajtova! Učitano: {key.Length}" });

        CryptoHelperNamespace.KeyManager.SaveKey(key);
        CryptoHelperNamespace.KeyManager.ApplyKey(key);

        return Results.Ok(new
        {
            success = true,
            message = "Ključ učitan i primenjen!",
            keyHex = BitConverter.ToString(key).Replace("-", "")
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});


app.MapGet("/api/current-key", () =>
{
    return Results.Ok(new
    {
        keyHex = BitConverter.ToString(CryptoHelperNamespace.CryptoHelper.EncryptionKey).Replace("-", "")
    });
});




app.Run();


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
