using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

if (args.Length < 1) throw new Exception("Missing test config file path");

var configFile = new FileInfo(args[0]);
if (!configFile.Exists) throw new Exception($"Test config file with path '{configFile.FullName}' not found");

var config = await GetConfig(configFile);

var proc = Process.Start(config.ServerPath, config.Arguments);
await Task.Delay(1000);

using var client = new HttpClient() { BaseAddress = new(config.BaseAddress) };

var queries = config.TestEntries.Select(async x => (Entry: x, Resp: await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, x.Url))));

var resps = await Task.WhenAll(queries);
var res = await Task.WhenAll(resps.Select(async x => (x.Entry, Errors: await CompareResponse(x.Entry, x.Resp))));

var errors = res.Where(x => x.Errors.Any());

if(errors.Any())
{
    throw new Exception($@"
For config:
  - Path: {configFile.FullName}
  - Server: {config.ServerPath} {string.Join(" ", config.Arguments)}
  - Base URL: {config.BaseAddress}
Got the following errors:
{string.Join("\n", errors.Select(x => $@"URL: {x.Entry}:
    {string.Join("\n", x.Errors.Select(y => $"    {y}"))}"))}
");
}


proc.Kill();

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

static bool TryValidateConfig(TestConfig? input, [NotNullWhen(true)] out TestConfig? output)
{
    output = null;
    if (input is null
        || string.IsNullOrWhiteSpace(input.ServerPath)
        || string.IsNullOrWhiteSpace(input.BaseAddress)
        || !input.TestEntries.Any()
        || input.TestEntries.Any(x => string.IsNullOrWhiteSpace(x.Url))
        || (new FileInfo(input.ServerPath) is var f && !f.Exists)
    ) { return false; }
    output = input with { ServerPath = f.FullName };
    return true;
}

static async Task<TestConfig> GetConfig(FileInfo configFile)
{
    using var fileStream = File.OpenRead(configFile.FullName);
    var cfg = await JsonSerializer.DeserializeAsync<TestConfig>(fileStream);
    if (!TryValidateConfig(cfg, out var cfg2)) throw new Exception("Invalid Config");
    return cfg2;
}

record TestConfig
{
    public string ServerPath { get; init; } = string.Empty;
    public IEnumerable<string> Arguments { get; init; } = Array.Empty<string>();
    public string BaseAddress {  get; init; } = string.Empty;
    public IEnumerable<TestEntry> TestEntries { get; init; } = Array.Empty<TestEntry>();
}
record TestEntry
{
    public string Url { get; init; } = string.Empty;
    public int StatusCode { get; init; } = 200;
    public string Content { get; init; } = string.Empty;
    public bool IsRegex { get; init; } = false;
}