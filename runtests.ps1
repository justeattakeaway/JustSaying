$ProjectDir = "."

$awstoolsintegration = "$ProjectDir\JustSaying.AwsTools.IntegrationTests\project.json"
& dotnet test $awstoolsintegration

$awstoolsunit = "$ProjectDir\JustSaying.AwsTools.UnitTests\project.json"
& dotnet test $awstoolsunit

$jsintegration = "$ProjectDir\JustSaying.IntegrationTests\project.json"
& dotnet test $jsintegration

$messagingunit = "$ProjectDir\JustSaying.Messaging.UnitTests\project.json"
& dotnet test $messagingunit

$jsunit = "$ProjectDir\JustSaying.UnitTests\project.json"
& dotnet test $jsunit