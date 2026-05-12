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

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("✅ Контроллер сообщений активен и готов к работе!");
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Update? update)
        {
            _logger.LogInformation("Telegram update received");

            if (update?.Message?.Text == null)
                return Ok();

            try
            {
                var chatId = update.Message.Chat.Id;
                var text = update.Message.Text;

                _logger.LogInformation($"Message: {text}");

                if (text == "/start")
                {
                    await _botClient.SendMessage(chatId, "✅ Связь установлена! Бот работает.");
                }
                else
                {
                    await _botClient.SendMessage(chatId, $"Вы написали: {text}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram processing error");
            }

            return Ok();
        }
    }
}
