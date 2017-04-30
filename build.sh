#!/bin/sh
export artifacts=$(dirname "$(readlink -f "$0")")/artifacts
export configuration=Release

dotnet restore JustSaying.sln --verbosity minimal || exit 1
dotnet build JustSaying/JustSaying.csproj --output $artifacts --configuration $configuration --framework "netstandard1.6" || exit 1
