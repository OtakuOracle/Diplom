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

        private string GetIcon(string name)
        {
            name = name.ToLower();

            if (name.Contains("лыж")) return "🎿";
            if (name.Contains("сноуборд")) return "🏂";
            if (name.Contains("перчат")) return "🧤";
            if (name.Contains("очк")) return "🥽";
            if (name.Contains("коньк")) return "⛸";
            if (name.Contains("шлем")) return "🪖";
            if (name.Contains("снегоход") || name.Contains("снегокат")) return "🛷";

            return "🎒";
        }

        private async Task SendInventory(long chatId)
        {
            var items = await _db.Inventories.ToListAsync();

            var buttons = items
                .Select(x => InlineKeyboardButton.WithCallbackData(
                    $"{GetIcon(x.InventoryName)} {x.InventoryName}",
                    $"inv_{x.InventoryId}"
                ))
                .Select(x => new[] { x })
                .ToList();

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.SendMessage(
                chatId,
                "🎿 Выберите инвентарь:",
                replyMarkup: keyboard
            );
        }

        [HttpPost]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromBody] Update? update)
        {
            try
            {
                if (update?.CallbackQuery != null)
                {
                    var chatId = update.CallbackQuery.Message.Chat.Id;
                    var data = update.CallbackQuery.Data;

                    if (data == "open_inventory")
                    {
                        await SendInventory(chatId);
                        return Ok();
                    }

                    if (data == "back_inventory")
                    {
                        await SendInventory(chatId);
                        return Ok();
                    }

                    if (data.StartsWith("inv_"))
                    {
                        var id = int.Parse(data.Replace("inv_", ""));
                        var item = await _db.Inventories.FindAsync(id);

                        if (item != null)
                        {
                            var icon = GetIcon(item.InventoryName);

                            var message =
                                $"{icon} {item.InventoryName}\n\n" +
                                $"Модель: {item.InventoryModel}\n" +
                                $"Размер: {item.InventorySize}\n" +
                                $"💰 Цена: {item.RentalCostPerHour} ₽ / час";

                            var keyboard = new InlineKeyboardMarkup(
                                InlineKeyboardButton.WithCallbackData(
                                    "⬅ Назад к списку",
                                    "back_inventory"
                                )
                            );

                            await _botClient.SendMessage(
                                chatId,
                                message,
                                replyMarkup: keyboard
                            );
                        }

                        return Ok();
                    }
                }

                if (update?.Message?.Text == null)
                    return Ok();

                var chatIdMessage = update.Message.Chat.Id;
                var text = update.Message.Text;

                if (text == "/start")
                {
                    var keyboard = new InlineKeyboardMarkup(
                        InlineKeyboardButton.WithCallbackData(
                            "🎿 Инвентарь",
                            "open_inventory"
                        )
                    );

                    await _botClient.SendMessage(
                        chatIdMessage,
                        "🏔 Добро пожаловать в сервис услуг и инвентаря горнолыжного курорта!",
                        replyMarkup: keyboard
                    );
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