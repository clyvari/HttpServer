using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) throw new Exception("Missing test config file path");

var configFile = new FileInfo(args[0]);
if (!configFile.Exists) throw new Exception($"Test config file with path '{configFile.FullName}' not found");

var config = await GetConfig(configFile);


await PlayTests(config, configFile);


Console.WriteLine("All queries where successfull !");


static async Task PlayTests(IEnumerable<TestConfig> tests, FileInfo configFile)
{
    foreach (var test in tests)
    {
        await PlayTest(test, configFile);
    }
}

static async Task PlayTest(TestConfig test, FileInfo configFile)
{
    using var proc = Process.Start(test.ServerPath, test.Arguments);
    try
    {
        await Task.Delay(1000);

        using var client = new HttpClient() { BaseAddress = new(test.BaseAddress) };

        var queries = test.TestEntries.Select(async x => (Entry: x, Resp: await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, x.Url))));

        var resps = await Task.WhenAll(queries);
        var res = await Task.WhenAll(resps.Select(async x => (x.Entry, Errors: await CompareResponse(x.Entry, x.Resp))));
        var errors = res.Where(x => x.Errors.Any());
        if (errors.Any())
        {
            throw new Exception($@"
For config:
  - Path: {configFile.FullName}
  - Server: {test.ServerPath} {string.Join(" ", test.Arguments)}
  - Base URL: {test.BaseAddress}
Got the following errors:
{string.Join("\n", errors.Select(x => $@"URL: {x.Entry}:
    {string.Join("\n", x.Errors.Select(y => $"    {y}"))}"))}
");
        }
    }
    finally
    {
        Console.WriteLine("All done, killing server..");
        proc.Kill();
    }
}

static async Task<IEnumerable<string>> CompareResponse(TestEntry entry, HttpResponseMessage response)
{
    var errors = new List<string>();
    
    var content = await response.Content.ReadAsStringAsync();

    if (entry.StatusCode != (int)response.StatusCode) errors.Add($"Status '{entry.StatusCode}' doesn't match status '{response.StatusCode}'");

    if(entry.IsRegex)
    {
        if (!Regex.IsMatch(content, entry.Content)) errors.Add($"Content doesn't match regex {entry.Content}");
    }
    else
    {
        if(content != entry.Content) errors.Add($"'{content}' doesn't match {entry.Content}");
    }

    return errors;
}

static bool TryValidateConfig(IEnumerable<TestConfig>? input, [NotNullWhen(true)] out List<TestConfig>? output)
{
    output = null;
    if (input?.Any() != true) return false;

    output = [];
    foreach (var cfg in input)
    {
        if (string.IsNullOrWhiteSpace(cfg.ServerPath)) throw new Exception(nameof(cfg.ServerPath));
        if (string.IsNullOrWhiteSpace(cfg.BaseAddress)) throw new Exception(nameof(cfg.BaseAddress));
        if (!cfg.TestEntries.Any()) throw new Exception(nameof(cfg.TestEntries) + " empty");
        if (cfg.TestEntries.Any(x => string.IsNullOrWhiteSpace(x.Url))) throw new Exception(nameof(TestEntry.Url));
        if (new FileInfo(cfg.ServerPath) is var f && !f.Exists) throw new Exception(nameof(cfg.ServerPath) + $" ({cfg.ServerPath} -> {f.FullName})");

        output.Add(cfg with { ServerPath = f.FullName });
    }

    return true;
}

static async Task<IEnumerable<TestConfig>> GetConfig(FileInfo configFile)
{
    using var fileStream = File.OpenRead(configFile.FullName);
    var cfg = await JsonSerializer.DeserializeAsync<TestConfig[]>(fileStream);
    if (!TryValidateConfig(cfg, out var cfg2)) throw new Exception("Invalid Config");
    return cfg2;
}

record TestConfig
{
    public string ServerPath { get; init; } = "";
    public IEnumerable<string> Arguments { get; init; } = [];
    public string BaseAddress {  get; init; } = "";
    public IEnumerable<TestEntry> TestEntries { get; init; } = [];
}
record TestEntry
{
    public string Url { get; init; } = "";
    public int StatusCode { get; init; } = 200;
    public string Content { get; init; } = "";
    public bool IsRegex { get; init; } = false;
}