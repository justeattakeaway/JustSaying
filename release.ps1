param(
  [Parameter(Mandatory=$true, HelpMessage="The version number to publish, eg 1.2.3. Set this in CI first.")]
  [string] $version,
  [Parameter(Mandatory=$false, HelpMessage="CI project owner")]
  [string] $owner = "justeattech"
)

if (($version -eq $null) -or ($version -eq '')) {
  # TODO: validate that a tag like this doesn't exist already
  throw "Must supply version number in semver format eg 1.2.3"
}
$manifest = get-content "deploy/manifest.json" -raw | ConvertFrom-Json
$ci_name = "je-$($manifest.feature.name)"
$ci_uri = "https://ci.appveyor.com/project/$owner/$ci_name"
$tag = "v$version"
# $release = "release-$version"
write-host "Your current status" -foregroundcolor green
& git status
write-host "Stashing any work and checking out master" -foregroundcolor green
& git stash
& git checkout master
& git pull upstream master --tags
write-host "We'll pause now while you remember to bump the version number in appveyor.yml to match the version you're releasing ($version) ;-)"
write-host "  TODO: bounty - do this in code against appveyor's api" -foregroundcolor red
write-host "  http://www.appveyor.com/docs/api/projects-builds#update-project" -foregroundcolor red
read-host "hit enter when you've done that..."
write-host "Tagging & branching. tag: $tag / branch: $release" -foregroundcolor green
& git tag -a $tag -m "Release $tag"
& git checkout $tag
write-host "Pushing" -foregroundcolor green
& git push --tags upstream
write-host "Done."
write-host "Check $ci_uri"
& git checkout master
write-host "Putting you back on master branch" -foregroundcolor green
exit 0