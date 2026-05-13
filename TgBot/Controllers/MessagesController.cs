using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Elbrus.Models;

namespace TgBot.Controllers
{
    [ApiController]
    [Route("messages")]
    public class MessagesController : ControllerBase
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ILogger<MessagesController> _logger;
        private readonly DiplomContext _db;

        public MessagesController(
            ITelegramBotClient botClient,
            ILogger<MessagesController> logger,
            DiplomContext db)
        {
            _botClient = botClient;
            _logger = logger;
            _db = db;
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

            try
            {
                // Обработка нажатий на кнопки
                if (update?.CallbackQuery != null)
                {
                    var chatId = update.CallbackQuery.Message.Chat.Id;
                    var data = update.CallbackQuery.Data;

                    if (data.StartsWith("inv_"))
                    {
                        var id = int.Parse(data.Replace("inv_", ""));

                        var item = await _db.Inventories.FindAsync(id);

                        if (item != null)
                        {
                            var message =
                                $"🎿 {item.InventoryName}\n\n" +
                                $"Модель: {item.InventoryModel}\n" +
                                $"Размер: {item.InventorySize}\n" +
                                $"💰 Цена: {item.RentalCostPerHour} ₽ / час";

                            await _botClient.SendMessage(chatId, message);
                        }
                    }

                    return Ok();
                }

                if (update?.Message?.Text == null)
                    return Ok();

                var chatIdMessage = update.Message.Chat.Id;
                var text = update.Message.Text;

                _logger.LogInformation($"Message: {text}");

                if (text == "/start")
                {
                    await _botClient.SendMessage(
                        chatIdMessage,
                        "🏔 Добро пожаловать в сервис услуг и инвентаря горнолыжного курорта!\n\nВведите /inventory чтобы посмотреть доступный инвентарь."
                    );
                }
                else if (text == "/inventory")
                {
                    var items = await _db.Inventories.ToListAsync();

                    if (!items.Any())
                    {
                        await _botClient.SendMessage(chatIdMessage, "Инвентарь пока не добавлен.");
                    }
                    else
                    {
                        var buttons = items
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.InventoryName,
                                $"inv_{x.InventoryId}"
                            ))
                            .Select(x => new[] { x })
                            .ToArray();

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.SendMessage(
                            chatIdMessage,
                            "🎿 Выберите инвентарь:",
                            replyMarkup: keyboard
                        );
                    }
                }
                else
                {
                    await _botClient.SendMessage(chatIdMessage, $"Вы написали: {text}");
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