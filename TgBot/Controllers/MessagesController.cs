using Microsoft.AspNetCore.Mvc;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Requests;  // Добавьте этот using

namespace TgBot.Controllers
{
    [ApiController]
    [Route("[controller]")] // Маршрут будет /messages
    public class MessagesController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<MessagesController> _logger; // Добавим логирование

        public MessagesController(ITelegramBotClient botClient, ILogger<MessagesController> logger)
        {
            _botClient = botClient;
            _logger = logger;
        }

        // Этот метод будет получать все обновления от Telegram
        [HttpPost] // Telegram отправляет обновления через POST
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            if (update == null)
            {
                _logger.LogWarning("Получен пустой Update.");
                return BadRequest(); // Возвращаем Bad Request, если обновление пустое
            }

            _logger.LogInformation($"Получен Update типа: {update.Type}");

            // Обрабатываем только сообщения
            if (update.Type == UpdateType.Message && update.Message != null)
            {
                var message = update.Message;
                var chatId = message.Chat.Id;
                var messageText = message.Text;

                _logger.LogInformation($"Получено сообщение от чата {chatId}: {messageText}");

                // Проверяем, является ли сообщение командой /start
                if (messageText != null && messageText.StartsWith("/start"))
                {
                    try
                    {
                        string welcomeMessage = "Добро пожаловать" +
                                                "\n\n*Что можно сделать:*";
                                               
                        await _botClient.SendMessage( // В версии 22.x метод часто называется просто SendMessage
                            chatId: chatId,
                            text: welcomeMessage,
                            parseMode: ParseMode.Markdown
                        );


                        _logger.LogInformation($"Приветственное сообщение успешно отправлено в чат {chatId}.");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Ошибка при отправке приветственного сообщения в чат {chatId}.");
                        // В случае ошибки, Telegram всё равно ожидает 200 OK
                    }
                }
                // Здесь можно добавить обработку других команд или обычных текстовых сообщений
                else
                {
                    // Пример: эхо-ответ для любых других сообщений
                    // await _botClient.SendTextMessageAsync(
                    //     chatId: chatId,
                    //     text: $"Вы сказали: {messageText}"
                    // );
                }
            }
            // Если вам нужно обрабатывать другие типы обновлений (например, колбэки от кнопок, редактирования сообщений и т.д.),
            // вы можете добавить здесь дополнительные блоки `if (update.Type == ...)`

            // Telegram ожидает, что вы вернете 200 OK, чтобы он знал, что обновление было успешно получено.
            return Ok();
        }


    }
}