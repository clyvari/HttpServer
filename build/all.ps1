dotnet tool restore

$version="$(dotnet nbgv get-version -v NuGetPackageVersion)"

dotnet publish .\HttpServer\Clyvari.HttpServer.csproj `
               -p:DebugType=None `
               -p:DebugSymbols=false `
               -c Release `
               -p:UseAppHost=false `
               -p:AssemblyNameSuffix="$version" `
               -o publish

# https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids
$(
    'win-x64'
    #'win-arm64',
    #'linux-x64',
    #'linux-musl-x64',
    #'linux-musl-arm64',
    #'linux-arm', # AOT unavailable
    #'linux-arm64',
    #'linux-bionic-arm64', # No runtime pack
    #'osx-x64',
    #'osx-arm64',
    #'ios-arm64',
    #'android-arm64'
) |% {
        dotnet publish .\HttpServer\Clyvari.HttpServer.csproj `
                       -p:PublishAot=true `
                       -p:DebugType=None `
                       -p:DebugSymbols=false `
                       -c Release `
                       -r $_ `
                       -p:AssemblyNameSuffix="$version" `
                       -o publish
    }

# https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids
$(
    #'win-x64',
    #'win-arm64', # Produces more than 1 file
    'linux-x64',
    'linux-musl-x64',
    'linux-musl-arm64',
    'linux-arm', # AOT unavailable
    'linux-arm64',
    #'linux-bionic-arm64', # No runtime pack
    'osx-x64',
    'osx-arm64'
    #'ios-arm64',
    #'android-arm64'
) |% {
         dotnet publish .\HttpServer\Clyvari.HttpServer.csproj `
                        -p:DebugType=None `
                        -p:DebugSymbols=false `
                        -c Release `
                        --self-contained `
                        -p:PublishTrimmed=true `
                        -p:PublishSingleFile=true `
                        -r $_ `
                        -p:AssemblyNameSuffix="$version" `
                        -o publish
     }