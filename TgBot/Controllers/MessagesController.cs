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

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            try
            {
                if (update.Message != null)
                {
                    var chatId = update.Message.Chat.Id;
                    var text = update.Message.Text;

                    if (text == "/start")
                    {
                        await _botClient.SendMessage(
                            chatId,
                            "🏔 Добро пожаловать!\nВведите /inventory чтобы посмотреть список доступного инвентаря."
                        );
                    }

                    if (text == "/inventory")
                    {
                        var items = await _db.Inventories.ToListAsync();

                        var buttons = items
                            .Select(i => InlineKeyboardButton.WithCallbackData(
                                i.InventoryName,
                                $"inventory_{i.InventoryId}"
                            ))
                            .Select(b => new[] { b })
                            .ToArray();

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.SendMessage(
                            chatId,
                            "🎿 Выберите инвентарь:",
                            replyMarkup: keyboard
                        );
                    }
                }

                if (update.CallbackQuery != null)
                {
                    var data = update.CallbackQuery.Data;
                    var chatId = update.CallbackQuery.Message.Chat.Id;

                    if (data.StartsWith("inventory_"))
                    {
                        var id = int.Parse(data.Replace("inventory_", ""));

                        var item = await _db.Inventories
                            .FirstOrDefaultAsync(x => x.InventoryId == id);

                        if (item != null)
                        {
                            var message =
                                $"🎿 Инвентарь: {item.InventoryName}\n\n" +
                                $"Модель: {item.InventoryModel}\n" +
                                $"Размер: {item.InventorySize}\n" +
                                $"Цена за час: {item.RentalCostPerHour} ₽";

                            await _botClient.SendMessage(chatId, message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telegram error");
            }

            return Ok();
        }
    }
}