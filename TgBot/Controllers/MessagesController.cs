using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;

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

        // Этот метод сработает, когда вы просто перейдете по ссылке в браузере
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("✅ Контроллер сообщений активен и готов к работе!");
        }

        // Этот метод сработает, когда Telegram пришлет сообщение
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            _logger.LogInformation("--- НОВОЕ СООБЩЕНИЕ ОТ TELEGRAM ---");

            if (update == null) return Ok();

            try
            {
                if (update.Message is { Text: { } messageText })
                {
                    var chatId = update.Message.Chat.Id;
                    _logger.LogInformation($"Текст: {messageText} от ID: {chatId}");

                    if (messageText == "/start")
                    {
                        await _botClient.SendMessage(chatId, "✅ Связь установлена! Бот работает.");
                    }
                    else 
                    {
                        await _botClient.SendMessage(chatId, $"Вы написали: {messageText}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Ошибка: {ex.Message}");
            }

            return Ok();
        }
    }
}
