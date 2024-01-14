using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JustSaying.AwsTools;
using Amazon;
using Shouldly;

namespace JustSaying.Extensions.DependencyInjection.AwsCore.Tests;

public class ClientFactoryTests
{
    private readonly string _credentialsFile = Path.Join(AppContext.BaseDirectory, "credentials");

    [Fact]
    public void ShouldReadConfigFromFile()
    {
        //Arrange
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSayingWithAwsConfig(config, (_) => { });

        //Act
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.EUWest1);

        //Assert
        snsClient.Config.ServiceURL.ShouldBe("http://test.test/");
        snsClient.Config.UseHttp.ShouldBeTrue();
    }

    [Fact]
    public void ShouldReadFromConfigValues()
    {
        //Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AWS:Profile"] = "local",
                ["AWS:ProfilesLocation"] = _credentialsFile,
                ["AWS:Region"] = "us-east-1",
                ["AWS:ServiceURL"] = "http://profile.test/",
                ["AWS:UseHttp"] = "true"
            })
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSayingWithAwsConfig(config, (_) => { });

        //Act
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.USEast1);

        //Assert
        snsClient.Config.RegionEndpoint.ShouldBeNull();
        snsClient.Config.ServiceURL.ShouldBe("http://profile.test/");
    }

    [Fact]
    public void CanSetCorrectRegion()
    {
        //Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AWS:Profile"] = "local",
                ["AWS:ProfilesLocation"] = _credentialsFile,
                ["AWS:Region"] = "us-east-1"
            })
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSayingWithAwsConfig(config, (_) => { });

        //Act
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();
        var sqsClient = clientFactory.GetSqsClient(RegionEndpoint.USEast1);
        var snsClient = clientFactory.GetSnsClient(RegionEndpoint.USEast1);

        //Assert
        sqsClient.Config.RegionEndpoint.ShouldBe(RegionEndpoint.USEast1);
        snsClient.Config.RegionEndpoint.ShouldBe(RegionEndpoint.USEast1);
    }

    [Fact]
    public void ThrowsExceptionIfRegionConfigDiffers()
    {
        //Arrange
        var config = new ConfigurationBuilder()
             .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["AWS:Profile"] = "local",
                ["AWS:ProfilesLocation"] = _credentialsFile,
                ["AWS:Region"] = "us-east-1"
            })
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSayingWithAwsConfig(config, (_) => { });

        //Act
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();

        //Assert
        Assert.Throws<ArgumentException>("region", () => clientFactory.GetSnsClient(RegionEndpoint.USEast2));
    }
}
