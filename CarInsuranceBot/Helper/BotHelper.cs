using Telegram.Bot.Types;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using CarInsuranceBot.Models;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using CarInsuranceBot.Intefaces;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace CarInsuranceBot.Helper
{
    public class BotHelper
    {
        private readonly ICustomerService _customerService;
        private readonly ITelegramBotClient _botClient;
        private readonly IMindeeService _mindeeService;
        private readonly OpenAIClient _openAIClient;
        private readonly ILogger<BotHelper> _logger;

        public BotHelper(ICustomerService customerService, ITelegramBotClient botClient, 
            IConfiguration configuration, IMindeeService mindeeService, ILogger<BotHelper> logger)
        {
            _customerService = customerService;
            _botClient = botClient;
            _openAIClient = new OpenAIClient(configuration["OpenAISecret"]);
            _mindeeService = mindeeService;
            _logger = logger;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message)
                    return;

                var customerId = update.Message.From!.Id;
                await _customerService.InitializeCustomerIfItNotExistAsync(customerId);

                if (message.Text is not null && message.Text.StartsWith("/"))
                {
                    await HandleCommandsAsync(message, cancellationToken, customerId);
                    return;
                };

                var userStage = await _customerService.GetCustomerCurrentStageAsync(customerId);

                await HandleUserMessageAsync(message, userStage, cancellationToken, customerId);
            }
            catch (Exception ex)
            {
                _logger.LogCritical($"{ex.GetType().Name} {ex.Message}");
                await _botClient.SendTextMessageAsync(
                    chatId: update.Message!.Chat.Id,
                    text: "A critical error has just occurred in the application. Please contact customer support to provide information that will help us improve our service.",
                    cancellationToken: cancellationToken);
            }
        }

        public Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            _logger.LogError(ErrorMessage);
            return Task.CompletedTask;
        }

        private Task HandleCommandsAsync(Message message, CancellationToken cancellationToken, long customerId)
        {
            return message.Text switch
            {
                "/start" => HandleDialogStartAsync(message.Text, message.Chat.Id, cancellationToken, customerId),
                "/help" => _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text:
                    """
                    /start - begins the insurance application process
                    /support - customer support contacts
                    /finish - stops the conversation with the bot and resets the insurance application progress
                    """,
                    cancellationToken: cancellationToken),
                "/support" => _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "To contact customer support, you can send a message to email customer.support@domain.com",
                    cancellationToken: cancellationToken),
                "/finish" => Task.WhenAll(
                    _customerService.DeleteCustomerAsync(customerId),
                    _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "Your progress is cleaned. You can start the conversation again using the /start command",
                        cancellationToken: cancellationToken)),
                _ => _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Unknown command. If you didn't intend to use a command, do not start your message with the \"/\" symbol",
                    cancellationToken: cancellationToken)
            };
        }

        private Task HandleUserMessageAsync(Message message,Stage userStage , CancellationToken cancellationToken, long customerId)
        {
            return (userStage switch
            {
                Stage.Greeting when message.Text is not null => HandleDialogStartAsync(message!.Text, message.Chat.Id, cancellationToken, customerId),
                Stage.Passport when message.Photo is not null => HandlePassportUploadingAsync(message!, customerId, cancellationToken),
                Stage.PassportConfirmation when message.Text is not null => HandlePassportCoinfirmationAsync(message!.Text, message.Chat.Id, cancellationToken, customerId),
                Stage.Title when message.Photo is not null => HandleTitleUploadingAsync(message!, customerId, cancellationToken),
                Stage.TitleConfirmation when message.Text is not null => HandleTitleCoinfirmationAsync(message!.Text, message.Chat.Id, cancellationToken, customerId),
                Stage.PriceQuotation when message.Text is not null => HandlePriceQuotationAsync(message!.Text, message.Chat.Id, cancellationToken, customerId),
                _ => HandleUnexpectedCaseAsync(message.Chat.Id, cancellationToken)
            });
        }

        private async Task HandleDialogStartAsync(string messageText, long chatId, CancellationToken cancellationToken, long customerId)
        {
            _logger.LogInformation($"Bot started a conversation in chat: {chatId}");
            string botResponse;
            if (string.Equals(messageText, "/start", StringComparison.OrdinalIgnoreCase))
            {
                botResponse = await GetReplyFromOpenAIAsync(
                    """
                    You are a bot for car insurance processing. Introduce yourself and say something like 
                    'To start the procedure, you need to upload a photo of your passport', mention that photo must be in compressed format
                    """);
                await _customerService.SetCustomerStageAsync(customerId, Stage.Passport);
            }
            else
            {
                botResponse = await GetReplyFromOpenAIAsync(
                    """
                    The bot received an unexpected message from the user.
                    Advise the user to use /start command to start the interaction.
                    """);
            }
           
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: botResponse,
                cancellationToken: cancellationToken);
        }

        private async Task HandleTitleUploadingAsync(Message message, long id, CancellationToken cancellationToken)
        {
            string responseMessage = string.Empty;
            try
            {
                var base64Image = await GetBase64EncodedPhotoAsync(message);
                var result = await _mindeeService.ParseTitleImageAsync(base64Image);
                responseMessage = result is null 
                    ? responseMessage 
                    : result.ToString();
                var customerData = await _customerService.GetCustomerByIdAsync(id);
                customerData.UpdateCustomersTitleDataAndStage(result!, Stage.TitleConfirmation);

                await Task.WhenAll(_customerService.UpdateCustomer(customerData),
                    _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: responseMessage,
                    cancellationToken: cancellationToken));

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Do you confirm this data? (yes/no)",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleExceptionDurringRequestProcessing(message, ex, cancellationToken);
            }
        }

        private async Task HandlePassportUploadingAsync(Message message, long id, CancellationToken cancellationToken)
        {
            try
            {
                var base64Image = await GetBase64EncodedPhotoAsync(message);
                var result = await _mindeeService.ParsePassportImageAsync(base64Image);
                string responseMessage = result is null 
                    ? string.Empty 
                    : result.ToString();

                var customerData = await _customerService.GetCustomerByIdAsync(id);
                customerData.UpdateCustomersPassportDataAndStage(result!, Stage.PassportConfirmation);

                await Task.WhenAll(_customerService.UpdateCustomer(customerData),
                _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: responseMessage,
                    cancellationToken: cancellationToken));

                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Do you confirm this data? (yes/no)",
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleExceptionDurringRequestProcessing(message, ex, cancellationToken);
            }
        }

        private async Task HandleTitleCoinfirmationAsync(string message, long chatId, CancellationToken cancellationToken, long id)
        {
            if (string.Equals(message, "yes", StringComparison.OrdinalIgnoreCase))
            {
                await Task.WhenAll(_botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: await GetReplyFromOpenAIAsync(
                        """
                        Tell the user that you are ready to proceed with the insurance application,
                        but it will cost $100. The user needs to answer only \"yes\" or \"no\".
                        """),
                    cancellationToken: cancellationToken),
                _customerService.SetCustomerStageAsync(id, Stage.PriceQuotation));
            }
            else if (string.Equals(message, "no", StringComparison.OrdinalIgnoreCase))
            {
                await Task.WhenAll(_botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: await GetReplyFromOpenAIAsync("Ask user to send the photo of the car title again"),
                    cancellationToken: cancellationToken),
                _customerService.SetCustomerStageAsync(id, Stage.Title));
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Unexpected message, please type \"yes\" or \"no\" to continue",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandlePassportCoinfirmationAsync(string message, long chatId, CancellationToken cancellationToken, long customerId)
        {
            if (string.Equals(message, "yes", StringComparison.OrdinalIgnoreCase))
            {
                await Task.WhenAll(_botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: await GetReplyFromOpenAIAsync("Tell the user that to continue, he needs to upload his car title, mention that photo must be in compressed format"),
                    cancellationToken: cancellationToken),
                _customerService.SetCustomerStageAsync(customerId, Stage.Title));
            }
            else if (string.Equals(message, "no", StringComparison.OrdinalIgnoreCase))
            {
                await Task.WhenAll(_botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: await GetReplyFromOpenAIAsync("Ask user to send the photo of the passport again"),
                    cancellationToken: cancellationToken),
                _customerService.SetCustomerStageAsync(customerId, Stage.Passport));
            }
            else
            {
                await  _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Unexpected message, please type \"yes\" or \"no\" to continue",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task HandlePriceQuotationAsync(string message, long chatId, CancellationToken cancellationToken, long customerId)
        {
            if (string.Equals(message, "yes", StringComparison.OrdinalIgnoreCase))
            {
                var customerData = await _customerService.GetCustomerByIdAsync(customerId);
                var result = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions
                {
                    Messages =
                    {
                        new ChatRequestSystemMessage(
                                """
                                Genereate message in the following format and replase stars by value from message :
                                Wonderfull,
                                Your Car Insurance
                                ----------------------------
                                Policy Number: *******
                                Full Name: *******
                                Passport Type and Number: *****
                                VechileIdentificationNumber = *******
                                ----------------------------
                                After that say thank you for choossing our service and mention that the conversion is over
                                """),
                        new ChatRequestUserMessage($"Generate a dummy car insurance policy document with the following data:\nPolicy Number: 123456789\nFull Name:{customerData.FullName}\nVechileIdentificationNumber: {customerData.VechileIdentificationNumber}\nPassport Type and Number: {customerData.PassTypeAndNumber}\nPrice: 100 USD")
                    },
                    MaxTokens = 150, 
                    DeploymentName = "gpt-3.5-turbo",
                    ChoiceCount = 1
                });

                await Task.WhenAll(_botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: result.Value.Choices[0].Message.Content,
                    cancellationToken: cancellationToken),
                    _customerService.SetCustomerStageAsync(customerId, Stage.Finish));
            }
            else if (string.Equals(message, "no", StringComparison.OrdinalIgnoreCase))
            {
                var openAIResponse = await GetReplyFromOpenAIAsync(
                    """
                    The user is not apply with insurance with price of 100$,
                    try to explain that there are no discounts now and he need to answer (yes/no) if he want to continue
                    """
                );
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: openAIResponse,
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Unexpected message, please type \"yes\" or \"no\" to continue",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandleUnexpectedCaseAsync(long chatId, CancellationToken cancellationToken)
        {
            return _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Please follow the bot's instructions.",
                cancellationToken: cancellationToken);
        }

        private async Task<string> GetBase64EncodedPhotoAsync(Message message)
        {
            var file = await _botClient.GetFileAsync(message.Photo![message.Photo.Count() - 1].FileId);
            using var imageStream = new MemoryStream();
            await _botClient.DownloadFileAsync(file.FilePath!, imageStream);
            return Convert.ToBase64String(imageStream.ToArray());
        }

        private Task<string> HandleServiceErrorAsync()
        {
            return GetReplyFromOpenAIAsync(
                    """
                    There are some errors during user's image processing,
                    tell the user to try repeate a litle bit later or contact with support
                    """
                );
        }

        private async Task<string> GetReplyFromOpenAIAsync(string systemMessage)
        {
            try
            {
                var openAIResponse = await _openAIClient.GetChatCompletionsAsync(new ChatCompletionsOptions
                {
                    Messages = { new ChatRequestSystemMessage(systemMessage), },
                    MaxTokens = 100,
                    DeploymentName = "gpt-3.5-turbo",
                    ChoiceCount = 1
                });
                return openAIResponse.Value.Choices[0].Message.Content;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during OpenAI API Request :{ex.GetType().Name} {ex.Message}");
                return "Some of our service is currently unavaliable, please contact with customer support";
            }
        }

        private async Task HandleExceptionDurringRequestProcessing(Message message, Exception ex, CancellationToken cancellationToken)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: await HandleServiceErrorAsync(),
                cancellationToken: cancellationToken);
            _logger.LogError($"Error during title processing: {ex.GetType().Name},{ex.Message}");
        }
    }
}