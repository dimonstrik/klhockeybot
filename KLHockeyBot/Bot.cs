using System;
using System.IO;
using System.Net;
using Telegram.Bot;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using KLHockeyBot;
using KLHockeyBot.Database;
using KLHockeyBot.Services;

Console.WriteLine("Starting Bot...");
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register Bot configuration
        services.Configure<BotConfiguration>(context.Configuration.GetSection("BotConfiguration"));

        // Register Bot database
        services.AddSingleton<BotDatabase>();
        var serviceProvider = services.BuildServiceProvider();
        var botConfiguration = serviceProvider.GetService<IOptions<BotConfiguration>>()?.Value;
        ArgumentNullException.ThrowIfNull(botConfiguration);
        if (!File.Exists(botConfiguration.DbFilePath) || (args.Length > 0 && args[0] == "init"))
        {
            try
            {
                if (File.Exists(botConfiguration.DbFilePath)) File.Delete(botConfiguration.DbFilePath);
                serviceProvider.GetService<BotDatabase>().Initialize();
            }
            catch (Exception e)
            {
                Console.WriteLine($"BotDatabase exception: {e.Message}");
            }
        }
        
        // Register named HttpClient to benefits from IHttpClientFactory
        // and consume it with ITelegramBotClient typed client.
        // More read:
        //  https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-5.0#typed-clients
        //  https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
        services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>(httpClient =>
            {
                TelegramBotClientOptions options = new(botConfiguration.BotToken);
                return new TelegramBotClient(options, httpClient);
            });

        //TODO change console events writer to logger
        services.AddScoped<CommandProcessor>();
        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddHostedService<PollingService>();

    })
    .Build();

// Ignore untrusted SSL certificates
ServicePointManager.ServerCertificateValidationCallback = KLHockeyBot.Network.Ssl.Validator;

AppDomain.CurrentDomain.ProcessExit += (_, _) => { host.StopAsync().Wait(); };
await host.RunAsync();