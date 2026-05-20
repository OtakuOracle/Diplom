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
        private static Dictionary<long, string> _userStates = new();
        private static Dictionary<long, int> _authorizedUsers = new();
        private static Dictionary<long, string> _tempEmail = new();


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
        public async Task<IActionResult> Post([FromBody] Update update)
        {
            try
            {

                if (update.Message != null && update.Message.Text != null)
                {
                    var chatId = update.Message.Chat.Id;
                    var text = update.Message.Text;

                    if (text == "/start")
                    {
                        _userStates[chatId] = "wait_email";

                        await _botClient.SendMessage(chatId,
                            "🏔 Добро пожаловать!\n\nВведите email:");

                        return Ok();
                    }

                    if (_userStates.ContainsKey(chatId))
                    {
                        var state = _userStates[chatId];

                        if (state == "wait_email")
                        {
                            _tempEmail[chatId] = text;
                            _userStates[chatId] = "wait_password";

                            await _botClient.SendMessage(chatId, "Введите пароль:");
                            return Ok();
                        }

                        if (state == "wait_password")
                        {
                            var email = _tempEmail[chatId];

                            var client = await _db.Clients
                                .FirstOrDefaultAsync(x => x.Email == email && x.Password == text);

                            if (client != null)
                            {
                                _authorizedUsers[chatId] = client.ClientId;
                                _userStates.Remove(chatId);

                                var keyboard = new InlineKeyboardMarkup(new[]
                                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🎿 Инвентарь","open_inventory")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🧑🏫 Услуги","open_services")
                    }
                });

                                await _botClient.SendMessage(
                                    chatId,
                                    $"✅ Вы вошли как {client.FullName}",
                                    replyMarkup: keyboard
                                );
                            }
                            else
                            {
                                await _botClient.SendMessage(chatId,
                                    "❌ Неверный email или пароль");
                            }

                            return Ok();
                        }
                    }
                }


                if (update.CallbackQuery != null)
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
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔑 Войти", "login")
                    }
                });

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🏔 Добро пожаловать в сервис бронирования!",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data == "login")
                    {
                        _userStates[chatId] = "wait_email";

                        await _botClient.SendMessage(chatId, "Введите email:");
                        return Ok();
                    }

                    if (data == "open_inventory")
                    {
                        var items = await _db.Inventories.ToListAsync();

                        var buttons = items
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.InventoryName,
                                $"inv_{x.InventoryId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[]
                        {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_start")
                });

                        var keyboard = new InlineKeyboardMarkup(buttons);

                        await _botClient.EditMessageText(
                            chatId,
                            messageId,
                            "🎿 Выберите инвентарь:",
                            replyMarkup: keyboard
                        );

                        return Ok();
                    }

                    if (data == "open_services")
                    {
                        var services = await _db.Services.ToListAsync();

                        var buttons = services
                            .Select(x => InlineKeyboardButton.WithCallbackData(
                                x.ServiceName,
                                $"srv_{x.ServiceId}"
                            ))
                            .Select(x => new[] { x })
                            .ToList();

                        buttons.Add(new[]
                        {
                    InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_start")
                });

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
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var inventoryId = int.Parse(data.Replace("inv_", ""));
                        var clientId = _authorizedUsers[chatId];

                        var order = new Order
                        {
                            ClientId = clientId,
                            DateCreate = DateOnly.FromDateTime(DateTime.Now),
                            TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                            TotalPrice = 0
                        };

                        _db.Orders.Add(order);
                        await _db.SaveChangesAsync();

                        var orderService = new OrderService
                        {
                            OrderId = order.OrderId,
                            ServiceId = null,
                            RentTime = 1,
                            OrderStatusId = 1
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        var item = await _db.InventoryItems
                            .FirstOrDefaultAsync(x => x.InventoryId == inventoryId);

                        if (item != null)
                        {
                            var orderInventory = new OrderInventory
                            {
                                InventoryItemId = item.InventoryItemId,
                                OrderServiceId = orderService.OrderServiceId,
                                RentTime = 1
                            };

                            _db.OrderInventories.Add(orderInventory);
                            await _db.SaveChangesAsync();
                        }

                        await _botClient.SendMessage(chatId, "✅ Инвентарь успешно забронирован!");

                        return Ok();
                    }

                    if (data.StartsWith("srv_"))
                    {
                        if (!_authorizedUsers.ContainsKey(chatId))
                        {
                            await _botClient.SendMessage(chatId, "Сначала авторизуйтесь.");
                            return Ok();
                        }

                        var serviceId = int.Parse(data.Replace("srv_", ""));
                        var clientId = _authorizedUsers[chatId];

                        var order = new Order
                        {
                            ClientId = clientId,
                            DateCreate = DateOnly.FromDateTime(DateTime.Now),
                            TimeCreate = TimeOnly.FromDateTime(DateTime.Now),
                            TotalPrice = 0
                        };

                        _db.Orders.Add(order);
                        await _db.SaveChangesAsync();

                        var orderService = new OrderService
                        {
                            OrderId = order.OrderId,
                            ServiceId = serviceId,
                            RentTime = 1,
                            OrderStatusId = 1
                        };

                        _db.OrderServices.Add(orderService);
                        await _db.SaveChangesAsync();

                        await _botClient.SendMessage(chatId, "✅ Услуга успешно забронирована!");

                        return Ok();
                    }
                }

                    return Ok();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Ok();
            }
        }
    }
}