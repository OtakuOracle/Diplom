using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Newtonsoft.Json;

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
        public async Task<IActionResult> Post([FromBody] object rawUpdate)
        {
            _logger.LogInformation(">>> ПРИШЕЛ ЗАПРОС ОТ ТЕЛЕГРАМ!");

            try
            {
                var json = rawUpdate.ToString();
                _logger.LogInformation($"JSON данных: {json}");

                var update = JsonConvert.DeserializeObject<Update>(json);

                if (update?.Message != null)
                {
                    _logger.LogInformation($"Текст сообщения: {update.Message.Text}");

                    if (update.Message.Text == "/start")
                    {
                        await _botClient.SendMessage(update.Message.Chat.Id, "Бот видит вас!");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ОШИБКА ОБРАБОТКИ: {ex.Message}");
            }

            return Ok();
        }
    }
}
