using CarInsuranceBot.Helper;
using CarInsuranceBot.Intefaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceBot.Services
{
    public class BotRunner : IBotRunner
    {
        private readonly ITelegramBotClient botClient;
        private readonly IConfiguration _configuration;
        private readonly ICustomerService _customerService;
        private readonly IMindeeService _mindeeService;
        private readonly ILogger<BotRunner> _logger;
        private readonly ILogger<BotHelper> _helperLogger;
        public BotRunner(IConfiguration configuration, ICustomerService customerService,
            IMindeeService mindeeService, ILogger<BotRunner> logger, ILogger<BotHelper> helperLogger)
        {
            _configuration = configuration;
            _customerService = customerService;
            _mindeeService = mindeeService;
            _logger = logger;
            _helperLogger = helperLogger;
            botClient = new TelegramBotClient(configuration["TelegramBotToken"]!);
        }

        /// <summary>
        /// Runs bot
        /// </summary>
        public void Run()
        {
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>()
            };

            using CancellationTokenSource cts = new();

            var helper = new BotHelper(_customerService, botClient, _configuration, _mindeeService, _helperLogger);
            botClient.StartReceiving(
                updateHandler: helper.HandleUpdateAsync,
                pollingErrorHandler: helper.HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );
            _logger.LogInformation("Bot started");
        }
    }
}