using Microsoft.Extensions.FileProviders;
using System.Text;

const string BaseDirKey = "BaseDir";
const string UrlsKey = "Urls";
const string HttpPortsKey = "HTTP_PORTS";
const string HttpsPortsKey = "HTTPS_PORTS";
const string ConfigFileKey = "config";
const string DefaultConfigFile = "httpserver.json";

string[] HelpSwitches = [ "-h", "--help" ];

Dictionary<string, (string[] ShortSwitches, string Description)> CommandLineSwitches = new()
{
    [BaseDirKey] = ([ "-d" ], "The base directory to serve content from"),
    [UrlsKey] = (["-u"], "A list of urls to listen to (see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#url-formats)"),
    [HttpPortsKey] = (["-p"], "A list of HTTP ports to listen to"),
    [HttpsPortsKey] = (["-ps"], "A list of HTTPS ports to listen to"),
    [ConfigFileKey] = (["-c"], $"The configuration file to read (appsettings.json, appsettings.{{ENV}}.json, {DefaultConfigFile} by default)"),
};

if (PrintHelp()) return;

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
{
    Args = args,
});

builder.WebHost.UseKestrelHttpsConfiguration();


builder.Configuration.AddCommandLine(args, CommandLineSwitches.SelectMany(x => x.Value.ShortSwitches.Select(y => (Key: y, Value: x.Key)))
                                                              .ToDictionary(x => x.Key, x => x.Value)
                     );

var configFile = new FileInfo(builder.Configuration.GetValue<string>(ConfigFileKey) ?? DefaultConfigFile).FullName;
builder.Configuration.AddJsonFile(configFile, optional: true);

var baseDir = new DirectoryInfo(builder.Configuration.GetValue<string>(BaseDirKey) ?? Directory.GetCurrentDirectory()).FullName;

var app = builder.Build();

var fOpts = new FileServerOptions
{
    EnableDirectoryBrowsing = true,
    RedirectToAppendTrailingSlash = true,
};

fOpts.StaticFileOptions.ServeUnknownFileTypes = true;
fOpts.StaticFileOptions.FileProvider = new PhysicalFileProvider(baseDir);

app.UseFileServer(fOpts);
app.UseStatusCodePages();

app.Lifetime.ApplicationStarted.Register(() =>
{
    app.Logger.LogInformation(@"Config file: {ConfigFile}
Serving files from: {BaseDir}", configFile, baseDir);
});

app.Run();

bool PrintHelp()
{
    if (!args.Intersect(HelpSwitches).Any()) return false;

    var sb = new StringBuilder();

    sb.AppendLine(@$"
A basic Http Server that serve static content in a directory.
Based on Kestrel (see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0)

Can be configugred by some command line options.
Additional configuration should be provided in the following files:
  - appsettings.json
  - appsettings.{{ENV}}.json
  - a config file specified by the -c option, or {DefaultConfigFile} in the current directory by default
And responds to classic ASP.Net Core options and env variables (ASPNETCORE_, DOTNET_, ...)

Example usage:
  HttpServer -d ../MyDir -p 8080 -c myconfig.json

Options:
");

    var switches = CommandLineSwitches.Select(x => (sw: string.Join("|", x.Value.ShortSwitches.Append($"--{x.Key.ToLowerInvariant()}")), descr: x.Value.Description));
    var maxSwitchLength = switches.Max(x => x.sw.Length);
    sb = switches.Aggregate(sb, (acc, x) => acc.AppendLine($"    {x.sw}:{new string(' ', maxSwitchLength - x.sw.Length + 1)}{x.descr}"));

    Console.WriteLine(sb.ToString());
    return true;
}