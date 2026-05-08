using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups; 

namespace TgBot.Controllers
{
    [ApiController]
    [Route("messages")]
    public class MessagesController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(ITelegramBotClient botClient, ILogger<MessagesController> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null) return Ok();

            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;

                if (message.Text.StartsWith("/start"))
                {
                    await SendWelcomeMenu(chatId);
                }
            }

            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
            {
                var callbackQuery = update.CallbackQuery;
                _logger.LogInformation($"Нажата кнопка: {callbackQuery.Data}");

                if (callbackQuery.Data == "help")
                {
                    await _botClient.SendMessage(callbackQuery.Message.Chat.Id, "Тут будет инструкция...");
                }

                return Ok();
            }

            return Ok();
        }

        private async Task SendWelcomeMenu(long chatId)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                // Первый ряд кнопок
                new []
                {
                    InlineKeyboardButton.WithCallbackData("❓ Что можно сделать", "help"),
                    InlineKeyboardButton.WithCallbackData("📚 Обзор", "review"),
                },

            });

            string welcomeText = "👋 *Добро пожаловать!*\n\nВыберите действие ниже:";

            try
            {
                await _botClient.SendMessage(
                    chatId: chatId,
                    text: welcomeText,
                    parseMode: ParseMode.Markdown,
                    replyMarkup: inlineKeyboard 
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке меню");
            }
        }
    }
}