using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Služi statičke fajlove iz wwwroot
app.UseStaticFiles();

// Redirect na index.html
app.MapGet("/", () => Results.Redirect("/index.html"));

// API endpoint - test
app.MapGet("/api/test", () => new { status = "radi", timestamp = DateTime.Now });

// Inicijalizuj ključeve na startu
byte[] key = new byte[16];
byte[] iv = new byte[8];
new Random().NextBytes(key);
new Random().NextBytes(iv);
CryptoHelperNamespace.CryptoHelper.EncryptionKey = key;
CryptoHelperNamespace.CryptoHelper.EncryptionIV = iv;

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
        var ip = form["ip"].ToString();
        var port = int.Parse(form["port"].ToString() ?? "5000");

        if (file == null)
            return Results.BadRequest(new { error = "Fajl nije poslat" });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        byte[] fileData = ms.ToArray();

        // Sačuvaj privremeno
        string tempPath = Path.Combine(Path.GetTempPath(), file.FileName);
        await File.WriteAllBytesAsync(tempPath, fileData);

        // Pošalji preko TCP-a
        Network.TCPClient.SendFile(tempPath, port);

        return Results.Ok(new { success = true, message = "Fajl poslat preko TCP-a" });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
});


app.Run();
