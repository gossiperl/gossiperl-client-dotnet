## Configuring for running unit tests

    git clone ...
    cd gossiperl-client-dotnet/

    apt-get install -y install mono-complete
    mozroots --import --sync
    cd .nuget/
    wget https://nuget.org/nuget.exe
    # is there a better way of doing this?
    wget http://headsigned.com/download/running-nuget-command-line-on-linux/Microsoft.Build.zip
    unzip Microsoft.Build.zip

    mkdir -p ../packages

    mono --runtime=v4.0 NuGet.exe install ../exec/packages.config -OutputDirectory ../packages
    mono --runtime=v4.0 NuGet.exe install ../lib/packages.config -OutputDirectory ../packages
    mono --runtime=v4.0 NuGet.exe install ../tests/packages.config -OutputDirectory ../packages

    cd ..
    # Build the solution:
    xbuild gossiperl-client-dotnet.sln

## Running unit tests

    nunit-console tests/gossiperl-client-tests.csproj