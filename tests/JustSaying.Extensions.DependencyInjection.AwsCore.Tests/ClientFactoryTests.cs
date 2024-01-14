using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using JustSaying.AwsTools;
using Amazon;

namespace JustSaying.Extensions.DependencyInjection.AwsCore.Tests;

public class ClientFactoryTests
{
    private readonly string _configFile = Path.Join(AppContext.BaseDirectory, "config");

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
        Assert.True(snsClient.Config.ServiceURL.Equals("http://test.test/") == true);
        Assert.True(snsClient.Config.UseHttp);
    }

    [Fact]
    public void ShouldReadFromConfigValues()
    {
        //Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"AWS:Profile", "local"},
                {"AWS:ProfilesLocation", _credentialsFile},
                {"AWS:Region", "us-east-1"},
                {"AWS:ServiceURL", "http://profile.test/"},
                {"AWS:UseHttp", "true"}
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
                {"AWS:Profile", "local"},
                {"AWS:ProfilesLocation", _credentialsFile},
                {"AWS:Region", "us-east-1"}
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
        Assert.True(sqsClient.Config.RegionEndpoint == RegionEndpoint.USEast1);
        Assert.True(snsClient.Config.RegionEndpoint == RegionEndpoint.USEast1);
    }

    [Fact]
    public void ThrowsExceptionIfRegionConfigDiffers()
    {
        //Arrange
        var config = new ConfigurationBuilder()
             .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"AWS:Profile", "local"},
                {"AWS:ProfilesLocation", _credentialsFile},
                {"AWS:Region", "us-east-1"}
            })
            .Build();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddJustSayingWithAwsConfig(config, (_) => { });

        //Act
        var provider = serviceCollection.BuildServiceProvider();
        var clientFactory = provider.GetRequiredService<IAwsClientFactory>();

        //Assert
        Assert.Throws<ArgumentException>(() => clientFactory.GetSnsClient(RegionEndpoint.USEast2));
    }
}
