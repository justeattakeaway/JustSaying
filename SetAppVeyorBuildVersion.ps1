function Generate-RandomCharacters {
  param (
      [int]$Length
  )
  $set    = "abcdefghijklmnopqrstuvwxyz0123456789".ToCharArray()
  $result = ""
  for ($x = 0; $x -lt $Length; $x++) {
      $result += $set | Get-Random
  }
  return $result
}

$versionPrefix = (Select-Xml -Path ".\version.props" -XPath "/Project/PropertyGroup/VersionPrefix" | Select-Object -ExpandProperty Node).InnerText
$versionSuffix = (Select-Xml -Path ".\version.props" -XPath "/Project/PropertyGroup/VersionSuffix" | Select-Object -First 1 -ExpandProperty Node).InnerText
$buildNumber = $env:APPVEYOR_BUILD_NUMBER

if ($env:APPVEYOR_PULL_REQUEST_NUMBER){
  $buildNumber += "-" + (Generate-RandomCharacters -Length 8)
}

if ($env:APPVEYOR_REPO_TAG -ne "true") {
  if ($versionSuffix -ne "") {
    $versionSuffix += "-build$buildNumber"
  }
  if ($versionSuffix -eq "") {
    $versionSuffix = "build$buildNumber"
  }
}

if ($versionSuffix -ne "") {
  $version = "$versionPrefix-$versionSuffix"
} else {
  $version = $versionPrefix
}

Update-AppveyorBuild -Version "$version"
