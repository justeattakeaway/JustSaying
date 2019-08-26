#!/usr/bin/env bash

root=$(cd "$(dirname "$0")"; pwd -P)
artifacts=$root/artifacts
configuration=Release

export CLI_VERSION=`cat ./global.json | grep -E '[0-9]\.[0-9]\.[a-zA-Z0-9\-]*' -o`
export DOTNET_INSTALL_DIR="$root/.dotnetcli"
export PATH="$DOTNET_INSTALL_DIR:$PATH"

dotnet_version=$(dotnet --version)

if [ "$dotnet_version" != "$CLI_VERSION" ]; then
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version "$CLI_VERSION" --install-dir "$DOTNET_INSTALL_DIR"
fi

if [ "$CI" != "" -a "$TRAVIS_OS_NAME" == "linux" ]; then
    goaws_tag=latest
    docker pull pafortin/goaws:$goaws_tag
    docker run -d --name goaws -p 4100:4100 pafortin/goaws:$goaws_tag
    export AWS_SERVICE_URL="http://localhost:4100"
fi

dotnet build JustSaying/JustSaying.csproj --output $artifacts --configuration $configuration --framework "netstandard2.0" || exit 1
dotnet build JustSaying.Extensions.DependencyInjection.Microsoft/JustSaying.Extensions.DependencyInjection.Microsoft.csproj --output $artifacts --configuration $configuration --framework "netstandard2.0" || exit 1
dotnet build JustSaying.Extensions.DependencyInjection.StructureMap/JustSaying.Extensions.DependencyInjection.StructureMap.csproj --output $artifacts --configuration $configuration --framework "netstandard2.0" || exit 1

dotnet test ./JustSaying.UnitTests/JustSaying.UnitTests.csproj --output $artifacts '--logger:Console;verbosity=quiet' || exit 1
dotnet test ./JustSaying.IntegrationTests/JustSaying.IntegrationTests.csproj --output $artifacts '--logger:Console;verbosity=quiet' || exit 1
