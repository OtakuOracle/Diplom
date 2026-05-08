using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Newtonsoft.Json;
using System.Text;

namespace TgBot.Controllers
{
    [ApiController]
    [Route("messages")] // Этот путь должен строго совпадать с концом URL в вебхуке
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
        public async Task<IActionResult> Post()
        {
            string body;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
            }

            _logger.LogInformation("--- НОВОЕ СООБЩЕНИЕ ОТ TELEGRAM ---");
            _logger.LogInformation($"RAW JSON: {body}");

            if (string.IsNullOrEmpty(body))
            {
                _logger.LogWarning("Пришел пустой запрос.");
                return Ok();
            }

            try
            {
                var update = JsonConvert.DeserializeObject<Update>(body);

                if (update == null) return Ok();

                if (update.Message != null && !string.IsNullOrEmpty(update.Message.Text))
                {
                    var chatId = update.Message.Chat.Id;
                    var messageText = update.Message.Text;

                    _logger.LogInformation($"Текст сообщения: {messageText} от ID: {chatId}");

                    if (messageText == "/start")
                    {
                        await _botClient.SendMessage(
                            chatId: chatId,
                            text: "✅ Связь установлена! Бот работает и видит ваши сообщения."
                        );
                    }
                    else 
                    {
                        await _botClient.SendMessage(chatId, $"Вы написали: {messageText}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"ОШИБКА В КОНТРОЛЛЕРЕ: {ex.Message}");
            }

            return Ok();
        }
    }
}