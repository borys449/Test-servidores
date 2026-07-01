var builder = DistributedApplication.CreateBuilder(args);

var bd = builder
    .AddSqlServer("bdserver")
    .AddDatabase("bd");

builder.Build().Run();
