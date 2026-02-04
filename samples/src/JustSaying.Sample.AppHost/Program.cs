var builder = DistributedApplication.CreateBuilder(args);

var localstack = builder.AddContainer("localstack", "localstack/localstack", "latest")
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway")
    .WithEnvironment("DEBUG", "1");

var localstackEndpoint = localstack.GetEndpoint("gateway");

builder.AddProject<Projects.JustSaying_Sample_Restaurant_OrderingApi>("ordering-api")
    .WaitFor(localstack)
    .WithEnvironment("AWSServiceUrl", localstackEndpoint)
    .WithEnvironment("AWSRegion", "eu-west-1");

builder.AddProject<Projects.JustSaying_Sample_Restaurant_KitchenConsole>("kitchen-console")
    .WaitFor(localstack)
    .WithEnvironment("AWSServiceUrl", localstackEndpoint)
    .WithEnvironment("AWSRegion", "eu-west-1");

builder.Build().Run();
