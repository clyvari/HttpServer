using Microsoft.Extensions.FileProviders;

const string BaseDirKey = "BaseDir";
const string UrlsKey = "Urls";
const string HttpPortsKey = "HTTP_PORTS";
const string HttpsPortsKey = "HTTPS_PORTS";

string[] HelpSwitches = [ "-h", "--help" ];

Dictionary<string, (string[] ShortSwitches, string Description)> CommandLineSwitches = new()
{
    [BaseDirKey] = ([ "-d" ], "The base directory to serve content from"),
    [UrlsKey] = (["-u"], "A list of urls to listen to (see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#url-formats)"),
    [HttpPortsKey] = (["-p"], "A list of HTTP ports to listen to"),
    [HttpsPortsKey] = (["-ps"], "A list of HTTPS ports to listen to"),
};

if (PrintHelp()) return;

var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
{
    Args = args,
});

builder.WebHost.UseKestrelHttpsConfiguration();

builder.Configuration.AddJsonFile("httpserver.json", optional: true)
                     .AddCommandLine(args, CommandLineSwitches.SelectMany(x => x.Value.ShortSwitches.Select(y => (Key: y, Value: x.Key)))
                                                              .ToDictionary(x => x.Key, x => x.Value)
                     );

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
    app.Logger.LogInformation("Serving files from: {BaseDir}", baseDir);
});

app.Run();

bool PrintHelp()
{
    if (!args.Intersect(HelpSwitches).Any()) return false;

    var switches = CommandLineSwitches.Select(x => (sw: string.Join("|", x.Value.ShortSwitches.Append($"--{x.Key.ToLowerInvariant()}")), descr: x.Value.Description));
    var maxSwitchLength = switches.Max(x => x.sw.Length);
    foreach (var (sw, descr) in switches)
    {
        Console.WriteLine($"{sw}:{new string(' ', maxSwitchLength-sw.Length+1)}{descr}");
    }

    return true;
}