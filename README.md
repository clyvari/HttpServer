A basic Http Server that serve static content in a directory.
Based on Kestrel (see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel?view=aspnetcore-8.0)

Can be configugred by some command line options.
Additional configuration should be provided in the following files:
  - `appsettings.json`
  - `appsettings.{ENV}.json`
  - a config file specified by the `-c` option, or `httpserver.json` in the current directory by default
And responds to classic ASP.Net Core options and env variables (`ASPNETCORE_`, `DOTNET_`, ...)

Example usage:
  `HttpServer -d ../MyDir -p 8080 -c myconfig.json`

By default, it can be run by just double-clicking (or `./HttpServer`) without any options, and it will serve the files in the current directory at http://localhost:5000/

Options:

    -d|--basedir:      The base directory to serve content from
    -u|--urls:         A list of urls to listen to (see: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#url-formats)
    -p|--http_ports:   A list of HTTP ports to listen to
    -ps|--https_ports: A list of HTTPS ports to listen to
    -c|--config:       The configuration file to read (appsettings.json, appsettings.{ENV}.json, httpserver.json by default)
