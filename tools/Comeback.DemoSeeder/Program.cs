namespace Comeback.DemoSeeder;

using System.Diagnostics;

/// <summary>
/// Puni pokrenuti stack demo podacima preko javnih API-ja (gateway + MailDev).
/// Pokretanje: dotnet run --project tools/Comeback.DemoSeeder [-- --gateway URL --maildev URL]
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var gateway = Option(args, "--gateway")
            ?? Environment.GetEnvironmentVariable("E2E_GATEWAY_URL")
            ?? "http://localhost:5000";
        var mailDev = Option(args, "--maildev")
            ?? Environment.GetEnvironmentVariable("E2E_MAILDEV_URL")
            ?? "http://localhost:1080";

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("Comeback demo seeder");
        Console.WriteLine($"  Gateway: {gateway}");
        Console.WriteLine($"  MailDev: {mailDev}");

        using var api = new ApiClient(gateway, mailDev);

        var probeError = await api.ProbeAsync();
        if (probeError is not null)
        {
            Console.Error.WriteLine(probeError);
            return 1;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await new Seeder(api).RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"{Environment.NewLine}GRESKA: {ex.Message}");
            Console.Error.WriteLine("Ponovni run nastavlja od mesta prekida (svaka faza je idempotentna).");
            return 1;
        }

        Console.WriteLine($"{Environment.NewLine}Gotovo za {stopwatch.Elapsed:mm\\:ss}. " +
            $"Prijava: marko.petrovic@demo.comeback.com / {DemoData.Password}");
        return 0;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
