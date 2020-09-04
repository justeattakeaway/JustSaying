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

dotnet build ./JustSaying.sln --output $artifacts --configuration $configuration || exit 1

dotnet test ./tests/JustSaying.UnitTests/JustSaying.UnitTests.csproj --output $artifacts --configuration $configuration '--logger:Console;verbosity=detailed;noprogress=true' || exit 1
dotnet test ./tests/JustSaying.Extensions.DependencyInjection.StructureMap.Tests/JustSaying.Extensions.DependencyInjection.StructureMap.Tests.csproj --output $artifacts --configuration $configuration '--logger:Console;noprogress=true' || exit 1
dotnet test ./tests/JustSaying.IntegrationTests/JustSaying.IntegrationTests.csproj --output $artifacts --configuration $configuration '--logger:Console;verbosity=normal;noprogress=true' || exit 1
