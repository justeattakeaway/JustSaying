using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JustSaying.AwsTools;
using Amazon;


[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TestProject1;


public class ClientFactoryTests
{
    [Fact]
    public void ShouldReadConfigFromFile()
    {

        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSyaingWithAwsConfig(config, (_) => { });
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.EUWest1);
        Assert.True(snsClient.Config.ServiceURL.Equals("http://test.test/") == true);
        Assert.True(snsClient.Config.UseHttp);
    }


    [Fact]
    public void ShouldReadFromDefaultProfile()
    {
        Environment.SetEnvironmentVariable("AWS_PROFILE", null);
        var bin = AppContext.BaseDirectory;
        Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", $"{bin}/config");
        Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", $"{bin}/credentials");
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSyaingWithAwsConfig(config, (_) => { });
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.EUWest1);
        Assert.True(string.IsNullOrEmpty(snsClient.Config.ServiceURL), snsClient.Config.ServiceURL);
        Assert.True(snsClient.Config.RegionEndpoint == RegionEndpoint.EUWest1);
        Assert.False(snsClient.Config.UseHttp);
        Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", null);
        Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", null);
        Environment.SetEnvironmentVariable("AWS_PROFILE", null);

    }

    [Fact]
    public void ShouldReadFromSpecifiedProfile()
    {
        var bin = AppContext.BaseDirectory;
        Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", $"{bin}/config");
        Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", $"{bin}/credentials");
        Environment.SetEnvironmentVariable("AWS_PROFILE", "local");
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSyaingWithAwsConfig(config, (_) => { });
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.USEast1);
        Assert.True(snsClient.Config.ServiceURL.Equals("http://profile.test/") == true);
    }


    [Fact]
    public void CanSetCorrectRegion()
    {
        var bin = AppContext.BaseDirectory;
        Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", $"{bin}/config");
        Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", $"{bin}/credentials");
        Environment.SetEnvironmentVariable("AWS_PROFILE", "local");
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSyaingWithAwsConfig(config, (_) => { });
        var provider = serviceCollection.BuildServiceProvider();

    }

    [Fact]
    public void ThrowsExceptionIfRegionConfigDiffers()
    {
        Environment.SetEnvironmentVariable("AWS_PROFILE", null);
        var bin = AppContext.BaseDirectory;
        Environment.SetEnvironmentVariable("AWS_CONFIG_FILE", $"{bin}/config");
        Environment.SetEnvironmentVariable("AWS_SHARED_CREDENTIALS_FILE", $"{bin}/credentials");
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSyaingWithAwsConfig(config, (_) => { });
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        Assert.Throws<ArgumentException>(() => clientFactory.GetSnsClient(RegionEndpoint.USEast1));
    }
}
