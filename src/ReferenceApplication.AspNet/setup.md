# ServiceComponents AspNet integration

## Dependency injection

- reference **Autofac.Extensions.DependencyInjection** package
- add **AutofacServiceProviderFactory** to host builder in *program.cs*

``` csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory()) // <- Add this line
    .ConfigureWebHostDefaults(webBuilder =>
    {
        webBuilder.UseStartup<Startup>();
    });
```

- add ConfigureContainer to *startup.cs*

``` csharp
public void ConfigureContainer(ContainerBuilder builder)
{
    // Add registrations here
}
```

More information: https://autofaccn.readthedocs.io/en/latest/integration/aspnetcore.html#asp-net-core-3-0-and-generic-hosting

## Logging

### Serilog integration

- reference **Serilog.AspNetCore** and **Serilog.Settings.Configuration** and **Serilog.Sinks.Seq** packages
- add **UseSerilog()** to *program.cs*

``` csharp
public static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .UseServiceProviderFactory(new AutofacServiceProviderFactory())
        .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)) // <- Add this line
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
```

More information: 
- https://github.com/nblumhardt/autofac-serilog-integration
- https://github.com/serilog/serilog-settings-configuration

### Sample configuration

#### Development

``` json
{
  "Serilog": {    
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "Microsoft": "Verbose",
        "System": "Warning",
        "NHibernate": "Verbose",
        "NHibernate.SQL": "Verbose"
      }
    },
    "WriteTo": [     
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss+fff}{EventType:x8} {Level:u3}][{Application}] {Message:lj} [{SourceContext}]{NewLine}{Exception}",
          "restrictedToMinimumLevel": "Information"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "restrictedToMinimumLevel": "Verbose"
        }
      }
    ]
  }
}
```

#### Production

``` json
{
  "Serilog": {    
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "Microsoft": "Warning",
        "System": "Error"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "[{Timestamp:HH:mm:ss+fff}{EventType:x8} {Level:u3}][{Application}] {Message:lj} [{SourceContext}]{NewLine}{Exception}",
          "restrictedToMinimumLevel": "Information"
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],    
    "Properties": {
      "Application": "REF-APP"
    }
  }
}
```