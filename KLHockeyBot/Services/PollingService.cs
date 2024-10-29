using System;
using KLHockeyBot.Abstract;
using Microsoft.Extensions.Logging;

namespace KLHockeyBot.Services;

// Compose Polling and ReceiverService implementations
public class PollingService(IServiceProvider serviceProvider, ILogger<PollingService> logger)
    : PollingServiceBase<ReceiverService>(serviceProvider, logger);
