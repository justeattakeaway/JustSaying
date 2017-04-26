#!/bin/sh
export artifacts=$(dirname "$(readlink -f "$0")")/artifacts
export configuration=Release

dotnet restore JustSaying.sln --verbosity minimal || exit 1
dotnet build JustSaying/JustSaying.csproj --output $artifacts --configuration $configuration --framework "netstandard1.6" || exit 1
dotnet build JustSaying.UnitTests/JustSaying.UnitTests.csproj --output $artifacts --configuration $configuration --framework "netcoreapp1.0" || exit 1
dotnet run --project JustSaying.UnitTests/JustSaying.UnitTests.csproj --configuration $configuration --framework "netcoreapp1.0"  || exit 1