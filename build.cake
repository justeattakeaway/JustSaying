var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var githubApiKey = Argument("githubApiKey", "");
var versionSuffix = Argument("versionSuffix", "");
var nugetSourceUrl = Argument("nugetSourceUrl", "");
var nugetApiKey = Argument("nugetApiKey", "");

var solutionPath = "./JustSaying.sln";
var isLocal = BuildSystem.IsLocalBuild;
var isRunningOnAppVeyor = AppVeyor.IsRunningOnAppVeyor;
var isPullRequest = AppVeyor.Environment.PullRequest.IsPullRequest;
var buildNumber = AppVeyor.Environment.Build.Number;
var buildResultDir = Directory("./bin");
var nugetRoot = buildResultDir + Directory("nuget");
var githubOwner = "justeat";
var githubRepo = "JustSaying";
var githubRawUri = "http://raw.githubusercontent.com";
string headSha = null;

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildResultDir);
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
  DotNetCoreRestore("./",new DotNetCoreRestoreSettings
	{
      Verbosity = DotNetCoreRestoreVerbosity.Verbose,
  });
});

Task("Build")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    var projects = GetFiles("./**/*.xproj");

    foreach(var project in projects)
    {
        DotNetCoreBuild(project.GetDirectory().FullPath, new DotNetCoreBuildSettings {
			Configuration = configuration,
			Verbose = false
        });
    }
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{

    var settings = new NUnit3Settings
    {
        Work = buildResultDir.Path.FullPath
    };

    if (isRunningOnAppVeyor)
    {
        settings.Where = "cat != ExcludeFromAppVeyor";
    }

    NUnit3("./**/bin/" + configuration + "/**/*.UnitTests.dll", settings);
});

Task("Create-Package")
    .IsDependentOn("Test")
    .Does(() =>
    {
        var projectFolders = new[] {
          "JustSaying",
          "JustSaying.AwsTools",
          "JustSaying.Messaging",
          "JustSaying.Models"
        };

        foreach (var projectFolder in projectFolders)
        {
            DotNetCorePack(projectFolder, new DotNetCorePackSettings{
                OutputDirectory = nugetRoot
        });
        }
    });


Task("Default")
    .IsDependentOn("Test");

RunTarget(target);
