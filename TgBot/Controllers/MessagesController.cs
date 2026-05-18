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

            if (name.Contains("урок") || name.Contains("обучен") || name.Contains("инструктор"))
                return "🧑🏫";

            if (name.Contains("экскурс"))
                return "🏔";

            if (name.Contains("гид"))
                return "🧭";

            if (name.Contains("сервис") || name.Contains("ремонт"))
                return "🛠";

            if (name.Contains("аренда"))
                return "📦";

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

        private async Task SendServices(long chatId, int messageId)
        {
            var services = await _db.Services.ToListAsync();

            var buttons = services
                .Select(x => InlineKeyboardButton.WithCallbackData(
                    $"{GetIcon(x.ServiceName)} {x.ServiceName}",
                    $"srv_{x.ServiceId}"
                ))
                .Select(x => new[] { x })
                .ToArray();

            var keyboard = new InlineKeyboardMarkup(buttons);

            await _botClient.EditMessageText(
                chatId,
                messageId,
                "🧑🏫 Выберите услугу:",
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
                    var messageId = update.CallbackQuery.Message.MessageId;
                    var data = update.CallbackQuery.Data;

                    if (data == "back_to_start")
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🎿 Инвентарь", "open_inventory")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🧑🏫 Услуги", "open_services")
                        }
                    });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🏔 Добро пожаловать в сервис услуг и инвентаря горнолыжного курорта!",
                            replyMarkup: keyboard
                        );
                        return Ok();
                    }

                    if (data == "open_inventory" || data == "back_inventory")
                    {
                        var items = await _db.Inventories.ToListAsync();

                        var buttons = items
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                $"{GetIcon(x.InventoryName)} {x.InventoryName}",
                                $"inv_{x.InventoryId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад к главному меню", "back_to_start") });

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🎿 Выберите инвентарь:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data == "open_services" || data == "back_services")
                    {
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                $"{GetIcon(x.ServiceName)} {x.ServiceName}",
                                $"srv_{x.ServiceId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад к главному меню", "back_to_start") });

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🧑🏫 Выберите услугу:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data.StartsWith("inv_"))
                    {
                        var inventoryId = int.Parse(data.Replace("inv_", ""));
                        var item = await _db.Inventories.FindAsync(inventoryId);

                        if (item != null)
                        {
                            var icon = GetIcon(item.InventoryName);

                            var message =
                                $"{icon} {item.InventoryName}\n\n" +
                                $"Модель: {item.InventoryModel}\n" +
                                $"💰 Цена: {item.RentalCostPerHour} ₽ / час";

                            var keyboard = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("📏 Размеры", $"sizes_{inventoryId}")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("⬅️ Назад к списку", "back_inventory")
                            }
                        });

                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                message,
                                replyMarkup: keyboard
                            );
                        }

                        return Ok();
                    }

                    if (data.StartsWith("sizes_"))
                    {
                        var inventoryId = int.Parse(data.Replace("sizes_", ""));

                        var sizes = await _db.InventoryItems
                            .Where(x => x.InventoryId == inventoryId)
                            .Include(x => x.InventoryStatus)
                            .ToListAsync();

                        if (sizes.Any())
                        {
                            var sizesTextBuilder = new System.Text.StringBuilder();

                            sizesTextBuilder.Append("📏 Размеры:\n\n");

                            foreach (var s in sizes)
                            {
                                string statusIcon;
                                if (s.InventoryStatus.InventoryStatusName.Contains("В наличии", StringComparison.OrdinalIgnoreCase))
                                {
                                    statusIcon = "✅";
                                }
                                else
                                {
                                    statusIcon = "❌";
                                }

                                sizesTextBuilder.AppendLine($"{statusIcon} Размер: {s.Size} — {s.InventoryStatus.InventoryStatusName}");
                            }

                            var keyboard = new InlineKeyboardMarkup(new[]
                            {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"inv_{inventoryId}")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("🏠 Назад к главному меню", "back_to_start")
                            }
                        });

                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                sizesTextBuilder.ToString(),
                                replyMarkup: keyboard
                            );
                        }
                        else
                        {
                            var keyboard = new InlineKeyboardMarkup(new[]
                            {
                            new[] { InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"inv_{inventoryId}") },
                            new[] { InlineKeyboardButton.WithCallbackData("🏠 Назад к главному меню", "back_to_start") }
                        });

                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                "📏 Размеры: \n\nВ данный момент информация о размерах отсутствует.",
                                replyMarkup: keyboard
                            );
                        }

                        return Ok();
                    }

                    if (data.StartsWith("srv_"))
                    {
                        var serviceId = int.Parse(data.Replace("srv_", ""));
                        var service = await _db.Services.FindAsync(serviceId);

                        if (service != null)
                        {
                            var icon = GetIcon(service.ServiceName);

                            var message =
                                $"{icon} {service.ServiceName}\n\n" +
                                $"💰 Цена: {service.CostPerHour} ₽ / час";

                            var keyboard = new InlineKeyboardMarkup(new[]
                            {
                                new[]
                                {
                                    InlineKeyboardButton.WithCallbackData("⬅️ Назад к услугам", "back_services"),
                                    InlineKeyboardButton.WithCallbackData("🏠 Назад к главному меню", "back_to_start")
                                }
                            });


                            await _botClient.EditMessageText(
                                chatId,
                                messageId,
                                message,
                                replyMarkup: keyboard
                            );
                        }

                        return Ok();
                    }
                }

                if (update?.Message?.Text != null)
                {
                    var chatIdMessage = update.Message.Chat.Id;
                    var text = update.Message.Text;

                    if (text == "/start")
                    {
                        var keyboard = new InlineKeyboardMarkup(new[]
                        {
                        new[]
                        {
                           InlineKeyboardButton.WithCallbackData("🎿 Инвентарь", "open_inventory")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🧑🏫 Услуги", "open_services")
                        }
                    });

                        await _botClient.SendMessage(
                            chatIdMessage,
                            "🏔 Добро пожаловать в сервис услуг и инвентаря горнолыжного курорта!",
                            replyMarkup: keyboard
                        );
                    }
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