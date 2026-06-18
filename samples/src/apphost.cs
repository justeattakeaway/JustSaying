#:sdk Aspire.AppHost.Sdk@13.4.2
#:property SignAssembly=false
#:project JustSaying.Sample.Restaurant.OrderingApi/JustSaying.Sample.Restaurant.OrderingApi.csproj
#:project JustSaying.Sample.Restaurant.KitchenConsole/JustSaying.Sample.Restaurant.KitchenConsole.csproj

var builder = DistributedApplication.CreateBuilder(args);

var floci = builder.AddContainer("floci", "floci/floci", "latest")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway");

var flociEndpoint = floci.GetEndpoint("gateway");

builder.AddProject<Projects.JustSaying_Sample_Restaurant_OrderingApi>("ordering-api")
    .WaitFor(floci)
    .WithEnvironment("AWSServiceUrl", flociEndpoint)
    .WithEnvironment("AWSRegion", "eu-west-1");

builder.AddProject<Projects.JustSaying_Sample_Restaurant_KitchenConsole>("kitchen-console")
    .WaitFor(floci)
    .WithEnvironment("AWSServiceUrl", flociEndpoint)
    .WithEnvironment("AWSRegion", "eu-west-1");

builder.Build().Run();
